namespace GorillaLocomotion
{
    using System.Collections.Generic;
    using System.Collections;
    using UnityEngine;
    using UnityEngine.InputSystem;
    using UnityEngine.XR;
    using Photon.VR.Player;

    public class Player : MonoBehaviour
    {
        [System.Serializable]
        public class JumpSettings
        {
            public float maxArmLength;
            public float unStickDistance;
            public float velocityLimit;
            public float maxJumpSpeed;
            public float minimumRaycastDistance;
        }

        private static Player _instance;

        RaycastHit hitInfo;
        public static Player Instance { get { return _instance; } }
        public static Vector3 lobbyRespawnPoint;
        public static Vector3 lobbyRespawnRotation;

        public JumpSettings[] jumpSettings;
        public SphereCollider headCollider;
        public CapsuleCollider bodyCollider;
        public Collider[] allColliders;
        public LayerMask normalViewMask;
        public LayerMask observerViewMask;
        List<Collider> activeColliders = new List<Collider>();

        public Transform leftHandFollower;
        public Transform rightHandFollower;

        public Transform rightHandTransform;
        public Transform leftHandTransform;

        private Vector3 lastLeftHandPosition;
        private Vector3 lastRightHandPosition;
        private Vector3 lastHeadPosition;

        private Vector3 lastLeftHandAttachmentPosition;
        private Vector3 lastRightHandAttachmentPosition;
        private Transform lastLeftHandHitObject;
        private Transform lastRightHandHitObject;
        public Transform playerLooker;

        public Rigidbody playerRigidBody;

        public int velocityHistorySize;
        public float maxArmLength = 1.5f;
        public float unStickDistance = 1f;

        public float velocityLimit;
        public float maxJumpSpeed;
        public float jumpMultiplier;
        public float minimumRaycastDistance = 0.05f;
        public float defaultSlideFactor = 0.03f;
        public float defaultPrecision = 0.995f;

        private Vector3[] velocityHistory;
        private int velocityIndex;
        private Vector3 currentVelocity;
        private Vector3 denormalizedVelocityAverage;
        private bool jumpHandIsLeft;
        private Vector3 lastPosition;

        public Vector3 rightHandOffset;
        public Vector3 leftHandOffset;

        public LayerMask locomotionEnabledLayers;

        public bool wasLeftHandTouching;
        public bool wasRightHandTouching;

        public bool disableMovement = false;
        public bool ignoreHandCollision = false;

        public AudioSource[] publicSoundSources;
        public AudioSource SnowHitSound;
        public AudioSource RockHitSound;
        public AudioSource MeatHitSound;
        public AudioSource MetalHitSound;
        public AudioSource WoodHitSound;

        PhotonVRPlayer myPlayer;

        public float hapticWaitSeconds = 0.05f;

        public float vibrationAmmount = 0.15f;

        [Header("Flying Player")]
        public Transform moveCenter;
        public float maxHorizontal;
        public float maxVertical;
        public float minVertical;
        public bool useObserver = false;

        public static bool useLocal = false;

        Vector3 lastHead;
        Vector3 lastLeftHand;
        Vector3 lastRightHand;
        Vector3 lastBase;
        Vector3 normalizedRight;
        Vector3 normalizedForward;
        Vector3 tempVec;
        Vector2 leftThumbstick;
        Vector2 rightThumbstick;
        float normalMod = 3.0f;
        float maxVertSpeed = 10.0f;
        float maxSpeed = 10.0f;
        float acceleration = 40.0f;

        Vector3 leftMove = Vector3.zero;
        Vector3 rightMove = Vector3.zero;
        float attachedObjectMove = 0.0f;

        #if UNITY_EDITOR
        public Vector3 bodyRotation = Vector3.zero;
        public Renderer[] renderersToDisable;
        public static bool editorMute = false;
        #endif

        int lastSource = -1;

        private void Awake()
        {
            #if UNITY_EDITOR
            if (PhotonRoyalePlayer.SceneIsRoyaleMode())
            {
                Cursor.lockState = CursorLockMode.Locked;

                leftHandTransform.position += Vector3.up * 0.5f;
                rightHandTransform.position += Vector3.up * 0.5f;
                leftHandTransform.position -= transform.right * 0.5f;
                rightHandTransform.position += transform.right * 0.5f;

                leftHandTransform.Rotate(-90.0f, 0.0f, 0.0f, Space.Self);
                rightHandTransform.Rotate(-90.0f, 0.0f, 0.0f, Space.Self);
            }
            #endif

            transform.localScale = Vector3.one;
            for (int i = 0; i < allColliders.Length; i++)
            {
                if (allColliders[i].enabled)
                {
                    activeColliders.Add(allColliders[i]);
                }
            }

            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex == 0)
            {
                if (lobbyRespawnPoint != Vector3.zero)
                {
                    transform.position = lobbyRespawnPoint;
                    transform.localEulerAngles = lobbyRespawnRotation;
                    lobbyRespawnPoint = Vector3.zero;
                }
            }

            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                _instance = this;
            }
            InitializeValues();
        }

        public void SwitchToObserver()
        {
            lastHead = headCollider.transform.position;
            lastLeftHand = leftHandTransform.position;
            lastRightHand = rightHandTransform.position;
            lastBase = playerRigidBody.transform.position;

            playerRigidBody.velocity = Vector3.zero;
            playerRigidBody.useGravity = false;
            DisableActiveColliders();
            PhotonRoyaleObserver.instance.photonView.RPC("ActivateObserver", Photon.Pun.RpcTarget.All);
            useObserver = true;
        }

        public void SwitchToNormal()
        {
            playerRigidBody.transform.position = lastBase;
            headCollider.transform.position = lastHead;
            leftHandTransform.position = lastLeftHand;
            rightHandTransform.position = lastRightHand;
            InitializeValues();

            playerRigidBody.velocity = Vector3.zero;
            playerRigidBody.useGravity = true;
            EnableActiveColliders();
            PhotonRoyaleObserver.instance.photonView.RPC("DisableObserver", Photon.Pun.RpcTarget.All);
            useObserver = false;
        }

        public void InitializeValues()
        {
            playerRigidBody = GetComponent<Rigidbody>();
            velocityHistory = new Vector3[velocityHistorySize];
            lastLeftHandPosition = leftHandFollower.transform.position;
            lastRightHandPosition = rightHandFollower.transform.position;
            lastHeadPosition = headCollider.transform.position;
            denormalizedVelocityAverage = Vector3.zero;
            currentVelocity = Vector3.zero;
            velocityIndex = 0;
            lastPosition = transform.position;
        }

        public void SetSettings(int setting)
        {
            maxArmLength = jumpSettings[setting].maxArmLength;
            unStickDistance = jumpSettings[setting].unStickDistance;
            velocityLimit = jumpSettings[setting].velocityLimit;
            maxJumpSpeed = jumpSettings[setting].maxJumpSpeed;
            minimumRaycastDistance = jumpSettings[setting].minimumRaycastDistance;
        }

        private Vector3 CurrentLeftHandPosition()
        {
            if ((PositionWithOffset(leftHandTransform, leftHandOffset) - headCollider.transform.position).magnitude < maxArmLength)
            {
                return PositionWithOffset(leftHandTransform, leftHandOffset);
            }
            else
            {
                return headCollider.transform.position + (PositionWithOffset(leftHandTransform, leftHandOffset) - headCollider.transform.position).normalized * maxArmLength;
            }
        }

        private Vector3 CurrentRightHandPosition()
        {
            if ((PositionWithOffset(rightHandTransform, rightHandOffset) - headCollider.transform.position).magnitude < maxArmLength)
            {
                return PositionWithOffset(rightHandTransform, rightHandOffset);
            }
            else
            {
                return headCollider.transform.position + (PositionWithOffset(rightHandTransform, rightHandOffset) - headCollider.transform.position).normalized * maxArmLength;
            }
        }

        private Vector3 PositionWithOffset(Transform transformToModify, Vector3 offsetVector)
        {
            return transformToModify.position + transformToModify.rotation * offsetVector;
        }

        private void Update()
        {
            #if UNITY_EDITOR
            if (PhotonRoyalePlayer.SceneIsRoyaleMode())
            {
                if (Keyboard.current.oKey.wasPressedThisFrame)
                {
                    if (useObserver)
                    {
                        SwitchToNormal();
                    }
                    else
                    {
                        SwitchToObserver();
                    }
                }

                Vector2 delta = Mouse.current.delta.ReadValue();
                Vector2 lookDelta = new Vector2(delta.y, delta.x) * 10.0f;
                bodyRotation += (Vector3)lookDelta * Time.smoothDeltaTime;
                bodyRotation.x = Mathf.Clamp(bodyRotation.x, -90.0f, 90.0f);
                transform.eulerAngles = bodyRotation;
            }

            if (Keyboard.current.mKey.wasPressedThisFrame)
            {
                editorMute = !editorMute;
            }
            #endif

            if (PhotonRoyalePlayer.SceneIsRoyaleMode() && PhotonRoyalePlayer.me != null && !useObserver && !PhotonRoyaleLobby.instance.activePlayersList.Contains(Photon.Pun.PhotonNetwork.LocalPlayer.ActorNumber))
            {
                if (PhotonRoyaleLobby.instance.AmIOutOfBounds(headCollider.transform.position))
                {
                    PhotonRoyalePlayer.me.ResetToAirship();
                }
            }
            else if (PhotonRoyalePlayer.SceneIsRoyaleMode() && PhotonRoyalePlayer.me != null && !useObserver && PhotonRoyaleLobby.instance.activePlayersList.Contains(Photon.Pun.PhotonNetwork.LocalPlayer.ActorNumber))
            {
                if (!PhotonRoyaleLobby.instance.AmIOutOfBounds(headCollider.transform.position) && Time.time - PhotonRoyaleLobby.instance.launchTime > 5.0f)
                {
                    PhotonRoyalePlayer.me.player.playerRigidBody.isKinematic = true;
                    PhotonRoyalePlayer.me.player.transform.position = PhotonRoyalePlayer.me.player.transform.position - Vector3.up * 5.0f;
                    PhotonRoyalePlayer.me.player.InitializeValues();
                    PhotonRoyalePlayer.me.player.EnableActiveColliders();
                    StartCoroutine(PhotonRoyalePlayer.me.Freeze());
                }
            }

            Camera.main.cullingMask = useObserver ? observerViewMask : normalViewMask;

            if (!disableMovement)
            {
                if (!useObserver)
                {
                    bool leftHandColliding = false;
                    bool rightHandColliding = false;
                    Vector3 finalPosition;
                    Vector3 rigidBodyMovement = Vector3.zero;
                    Vector3 firstIterationLeftHand = Vector3.zero;
                    Vector3 firstIterationRightHand = Vector3.zero;

                    if (lastLeftHandHitObject != null && Vector3.Distance(transform.position, lastLeftHandHitObject.position) > maxArmLength * 2.0f)
                    {
                        lastLeftHandHitObject = null;
                    }

                    if (lastRightHandHitObject != null && Vector3.Distance(transform.position, lastRightHandHitObject.position) > maxArmLength * 2.0f)
                    {
                        lastRightHandHitObject = null;
                    }

                    leftMove = lastLeftHandHitObject != null ? lastLeftHandHitObject.position - lastLeftHandAttachmentPosition : 
                                    lastRightHandHitObject != null ? lastRightHandHitObject.position - lastRightHandAttachmentPosition : Vector3.zero;
                    rightMove = lastRightHandHitObject != null ? lastRightHandHitObject.position - lastRightHandAttachmentPosition : 
                                    lastLeftHandHitObject != null ? lastLeftHandHitObject.position - lastLeftHandAttachmentPosition : Vector3.zero;

                    lastLeftHandPosition += leftMove;
                    lastRightHandPosition += rightMove;
                    lastPosition += leftMove;
                    attachedObjectMove = leftMove.magnitude > rightMove.magnitude ? leftMove.magnitude : rightMove.magnitude;

                    if (lastLeftHandHitObject != null)
                    {
                        lastLeftHandAttachmentPosition = lastLeftHandHitObject.position;
                    }

                    if (lastRightHandHitObject != null)
                    {
                        lastRightHandAttachmentPosition = lastRightHandHitObject.position;
                    }

                    bodyCollider.transform.eulerAngles = new Vector3(0, headCollider.transform.eulerAngles.y, 0);

                    //left hand

                    Vector3 distanceTraveled = CurrentLeftHandPosition() - lastLeftHandPosition + Physics.gravity * 2f * Time.deltaTime * Time.deltaTime;

                    leftHandFollower.localRotation = leftHandTransform.localRotation;

                    if (IterativeCollisionSphereCast(lastLeftHandPosition, minimumRaycastDistance, distanceTraveled, defaultPrecision, out finalPosition, true))
                    {
                        //this lets you stick to the position you touch, as long as you keep touching the surface this will be the zero point for that hand
                        if (wasLeftHandTouching)
                        {
                            firstIterationLeftHand = lastLeftHandPosition - CurrentLeftHandPosition();
                        }
                        else
                        {
                            firstIterationLeftHand = finalPosition - CurrentLeftHandPosition();

                            StartVibration(true, vibrationAmmount, 0.15f);
                            if (hitInfo.transform != null)
                            {
                                if ((hitInfo.transform.name != "TippyTip" && hitInfo.transform.GetComponentInParent<PhotonVRPlayer>() == null))
                                {
                                    lastLeftHandAttachmentPosition = hitInfo.transform.position;
                                    lastLeftHandHitObject = hitInfo.transform;
                                }
                                
                                if (hitInfo.transform.gameObject.tag == "ROCK" && !RockHitSound.isPlaying)
                                {
                                    BroadcastSound((int)PhotonVRPlayer.SoundType.Rock);
                                    RockHitSound.pitch = Random.Range(0.95f, 1.02f);
                                    RockHitSound.volume = Random.Range(0.33f, 0.345f);
                                    RockHitSound.Play();
                                }
                                if (hitInfo.transform.gameObject.tag == "WOOD" && !WoodHitSound.isPlaying)
                                {
                                    BroadcastSound((int)PhotonVRPlayer.SoundType.Wood);
                                    WoodHitSound.pitch = Random.Range(0.90f, 0.98f);
                                    WoodHitSound.volume = Random.Range(0.055f, 0.07f);
                                    WoodHitSound.Play();
                                }
                                if (hitInfo.transform.gameObject.tag == "METAL" && !MetalHitSound.isPlaying)
                                {
                                    BroadcastSound((int)PhotonVRPlayer.SoundType.Metal);
                                    MetalHitSound.pitch = Random.Range(0.97f, 1.01f);
                                    MetalHitSound.volume = Random.Range(0.06f, 0.08f);
                                    MetalHitSound.Play();
                                }
                                if (hitInfo.transform.gameObject.tag == "MEAT" && !MeatHitSound.isPlaying)
                                {
                                    BroadcastSound((int)PhotonVRPlayer.SoundType.Meat);
                                    MeatHitSound.pitch = Random.Range(0.97f, 1.01f);
                                    MeatHitSound.volume = Random.Range(0.06f, 0.08f);
                                    MeatHitSound.Play();
                                }
                                if (hitInfo.transform.gameObject.tag == "SNOW" && !SnowHitSound.isPlaying)
                                {
                                    BroadcastSound((int)PhotonVRPlayer.SoundType.Snow);
                                    SnowHitSound.pitch = Random.Range(0.89f, 1f);
                                    SnowHitSound.volume = Random.Range(0.06f, 0.08f);
                                    SnowHitSound.Play();
                                }
                            }
                        }
                        playerRigidBody.velocity = Vector3.zero;

                        leftHandColliding = true;
                    }

                    //right hand

                    distanceTraveled = CurrentRightHandPosition() - lastRightHandPosition + Physics.gravity * 2f * Time.deltaTime * Time.deltaTime;

                    rightHandFollower.localRotation = rightHandTransform.localRotation;

                    if (IterativeCollisionSphereCast(lastRightHandPosition, minimumRaycastDistance, distanceTraveled, defaultPrecision, out finalPosition, true))
                    {
                        if (wasRightHandTouching)
                        {
                            firstIterationRightHand = lastRightHandPosition - CurrentRightHandPosition();
                        }
                        else
                        {
                            firstIterationRightHand = finalPosition - CurrentRightHandPosition();

                            StartVibration(false, vibrationAmmount, 0.15f);
                            if (hitInfo.transform != null)
                            {
                                if ((hitInfo.transform.name != "TippyTip" && hitInfo.transform.GetComponentInParent<PhotonVRPlayer>() == null))
                                {
                                    lastRightHandAttachmentPosition = hitInfo.transform.position;
                                    lastRightHandHitObject = hitInfo.transform;
                                }

                                if (hitInfo.transform.gameObject.tag == "ROCK" && !RockHitSound.isPlaying)
                                {
                                    BroadcastSound((int)PhotonVRPlayer.SoundType.Rock);
                                    RockHitSound.pitch = Random.Range(0.95f, 1.02f);
                                    RockHitSound.volume = Random.Range(0.188f, 0.2f);
                                    RockHitSound.Play();
                                }
                                if (hitInfo.transform.gameObject.tag == "WOOD" && !WoodHitSound.isPlaying)
                                {
                                    BroadcastSound((int)PhotonVRPlayer.SoundType.Wood);
                                    WoodHitSound.pitch = Random.Range(0.90f, 0.98f);
                                    WoodHitSound.volume = Random.Range(0.055f, 0.07f);
                                    WoodHitSound.Play();
                                }
                                if (hitInfo.transform.gameObject.tag == "METAL" && !MetalHitSound.isPlaying)
                                {
                                    BroadcastSound((int)PhotonVRPlayer.SoundType.Metal);
                                    MetalHitSound.pitch = Random.Range(0.97f, 1.01f);
                                    MetalHitSound.volume = Random.Range(0.06f, 0.08f);
                                    MetalHitSound.Play();
                                }
                                if (hitInfo.transform.gameObject.tag == "MEAT" && !MeatHitSound.isPlaying)
                                {
                                    BroadcastSound((int)PhotonVRPlayer.SoundType.Meat);
                                    MeatHitSound.pitch = Random.Range(0.97f, 1.01f);
                                    MeatHitSound.volume = Random.Range(0.06f, 0.08f);
                                    MeatHitSound.Play();
                                }
                                if (hitInfo.transform.gameObject.tag == "SNOW" && !SnowHitSound.isPlaying)
                                {
                                    BroadcastSound((int)PhotonVRPlayer.SoundType.Snow);
                                    SnowHitSound.pitch = Random.Range(0.89f, 1f);
                                    SnowHitSound.volume = Random.Range(0.06f, 0.08f);
                                    SnowHitSound.Play();
                                }
                            }
                        }

                        playerRigidBody.velocity = Vector3.zero;

                        rightHandColliding = true;
                    }

                    //average or add

                    if ((leftHandColliding || wasLeftHandTouching) && (rightHandColliding || wasRightHandTouching))
                    {
                        //this lets you grab stuff with both hands at the same time
                        rigidBodyMovement = (firstIterationLeftHand + firstIterationRightHand) / 2;
                    }
                    else
                    {
                        rigidBodyMovement = firstIterationLeftHand + firstIterationRightHand;
                    }

                    //check valid head movement

                    if (IterativeCollisionSphereCast(lastHeadPosition, headCollider.radius, headCollider.transform.position + rigidBodyMovement - lastHeadPosition, defaultPrecision, out finalPosition, false))
                    {
                        rigidBodyMovement = finalPosition - lastHeadPosition;
                        //last check to make sure the head won't phase through geometry
                        if (Physics.Raycast(lastHeadPosition, headCollider.transform.position - lastHeadPosition + rigidBodyMovement, out hitInfo, (headCollider.transform.position - lastHeadPosition + rigidBodyMovement).magnitude + headCollider.radius * defaultPrecision * 0.999f, locomotionEnabledLayers.value))
                        {
                            rigidBodyMovement = lastHeadPosition - headCollider.transform.position;
                        }
                    }

                    if (rigidBodyMovement != Vector3.zero)
                    {
                        transform.position = transform.position + rigidBodyMovement;
                    }

                    lastHeadPosition = headCollider.transform.position;

                    //do final left hand position

                    distanceTraveled = CurrentLeftHandPosition() - lastLeftHandPosition;

                    if (IterativeCollisionSphereCast(lastLeftHandPosition, minimumRaycastDistance, distanceTraveled, defaultPrecision, out finalPosition, !((leftHandColliding || wasLeftHandTouching) && (rightHandColliding || wasRightHandTouching))))
                    {
                        lastLeftHandPosition = finalPosition;
                        leftHandColliding = true;
                    }
                    else
                    {
                        lastLeftHandPosition = CurrentLeftHandPosition();
                    }

                    //do final right hand position

                    distanceTraveled = CurrentRightHandPosition() - lastRightHandPosition;

                    if (IterativeCollisionSphereCast(lastRightHandPosition, minimumRaycastDistance, distanceTraveled, defaultPrecision, out finalPosition, !((leftHandColliding || wasLeftHandTouching) && (rightHandColliding || wasRightHandTouching))))
                    {
                        lastRightHandPosition = finalPosition;
                        rightHandColliding = true;
                    }
                    else
                    {
                        lastRightHandPosition = CurrentRightHandPosition();
                    }

                    StoreVelocities();

                    if ((rightHandColliding || leftHandColliding) && !disableMovement)
                    {
                        if (denormalizedVelocityAverage.magnitude > velocityLimit + attachedObjectMove)
                        {
                            if (denormalizedVelocityAverage.magnitude * jumpMultiplier > maxJumpSpeed + attachedObjectMove)
                            {
                                playerRigidBody.velocity = denormalizedVelocityAverage.normalized * (maxJumpSpeed + attachedObjectMove);
                            }
                            else
                            {
                                playerRigidBody.velocity = jumpMultiplier * denormalizedVelocityAverage;
                            }
                        }
                    }

                    //check to see if left hand is stuck and we should unstick it

                    if (ignoreHandCollision || (leftHandColliding && (CurrentLeftHandPosition() - lastLeftHandPosition).magnitude > unStickDistance && !Physics.SphereCast(headCollider.transform.position, minimumRaycastDistance * defaultPrecision, CurrentLeftHandPosition() - headCollider.transform.position, out hitInfo, (CurrentLeftHandPosition() - headCollider.transform.position).magnitude - minimumRaycastDistance, locomotionEnabledLayers.value)))
                    {
                        lastLeftHandPosition = CurrentLeftHandPosition();
                        leftHandColliding = false;
                        lastLeftHandHitObject = null;
                    }

                    //check to see if right hand is stuck and we should unstick it

                    if (ignoreHandCollision || (rightHandColliding && (CurrentRightHandPosition() - lastRightHandPosition).magnitude > unStickDistance && !Physics.SphereCast(headCollider.transform.position, minimumRaycastDistance * defaultPrecision, CurrentRightHandPosition() - headCollider.transform.position, out hitInfo, (CurrentRightHandPosition() - headCollider.transform.position).magnitude - minimumRaycastDistance, locomotionEnabledLayers.value)))
                    {
                        lastRightHandPosition = CurrentRightHandPosition();
                        rightHandColliding = false;
                        lastRightHandHitObject = null;
                    }

                    leftHandFollower.position = lastLeftHandPosition;
                    rightHandFollower.position = lastRightHandPosition;

                    wasLeftHandTouching = leftHandColliding;
                    wasRightHandTouching = rightHandColliding;
                }
                else // OBSERVER MOVEMENT
                {
                    playerRigidBody.useGravity = false;
                    switch (PhotonRoyaleObserver.observerMode)
                    {
                        case PhotonRoyaleObserver.ObserverMode.Thumbsticks:
                        {
                            leftThumbstick = InputManager.instance.GetLeftThumbstick();
                            rightThumbstick = InputManager.instance.GetRightThumbstick();

                            if (leftThumbstick.magnitude > 0.1f || rightThumbstick.magnitude > 0.1f)
                            {
                                if (rightThumbstick.magnitude < 0.1f)
                                {
                                    playerRigidBody.velocity = new Vector3(playerRigidBody.velocity.x, playerRigidBody.velocity.y * (1.0f - Time.deltaTime * normalMod), playerRigidBody.velocity.z);
                                }
                                else
                                {
                                    #if UNITY_EDITOR
                                    playerRigidBody.velocity += Vector3.up * rightThumbstick.y * acceleration * 0.0625f * Time.deltaTime;
                                    if (Mathf.Abs(playerRigidBody.velocity.y) > maxVertSpeed * 2.0f)
                                    {
                                        playerRigidBody.velocity = new Vector3(playerRigidBody.velocity.x, playerRigidBody.velocity.y > 0.0f ? maxVertSpeed * 2.0f : -maxVertSpeed * 2.0f, playerRigidBody.velocity.z);
                                    }
                                    #else
                                    playerRigidBody.velocity += Vector3.up * rightThumbstick.y * acceleration * Time.deltaTime;
                                    if (Mathf.Abs(playerRigidBody.velocity.y) > maxVertSpeed)
                                    {
                                        playerRigidBody.velocity = new Vector3(playerRigidBody.velocity.x, playerRigidBody.velocity.y > 0.0f ? maxVertSpeed : -maxVertSpeed, playerRigidBody.velocity.z);
                                    }
                                    #endif
                                }
                                

                                normalizedRight = headCollider.transform.right;
                                normalizedRight.y = 0.0f;
                                normalizedRight = normalizedRight.normalized;

                                normalizedForward = headCollider.transform.forward;
                                normalizedForward.y = 0.0f;
                                normalizedForward = normalizedForward.normalized;

                                playerRigidBody.velocity += normalizedRight * leftThumbstick.x * acceleration * Time.deltaTime;
                                playerRigidBody.velocity += normalizedForward * leftThumbstick.y * acceleration * Time.deltaTime;

                                tempVec.x = playerRigidBody.velocity.x;
                                tempVec.y = playerRigidBody.velocity.z;
                                tempVec.z = 0.0f;
                                if (tempVec.magnitude > maxSpeed)
                                {
                                    tempVec = tempVec.normalized * maxSpeed;
                                }
                                playerRigidBody.velocity = new Vector3(tempVec.x, playerRigidBody.velocity.y, tempVec.y);
                            }
                            else
                            {
                                playerRigidBody.velocity = playerRigidBody.velocity * (1.0f - Time.deltaTime * normalMod);
                            }
                            break;
                        }

                        case PhotonRoyaleObserver.ObserverMode.Look_and_Grip:
                        {
                            if (InputManager.instance.leftHandGrip.IsPressed() || InputManager.instance.rightHandGrip.IsPressed())
                            {
                                playerRigidBody.velocity += headCollider.transform.forward * Time.deltaTime * acceleration;
                                if (Mathf.Abs(playerRigidBody.velocity.y) > maxVertSpeed)
                                {
                                    playerRigidBody.velocity = new Vector3(playerRigidBody.velocity.x, playerRigidBody.velocity.y > 0.0f ? maxVertSpeed : -maxVertSpeed, playerRigidBody.velocity.z);
                                }

                                tempVec.x = playerRigidBody.velocity.x;
                                tempVec.y = playerRigidBody.velocity.z;
                                tempVec.z = 0.0f;
                                if (tempVec.magnitude > maxSpeed)
                                {
                                    tempVec = tempVec.normalized * maxSpeed;
                                }
                                playerRigidBody.velocity = new Vector3(tempVec.x, playerRigidBody.velocity.y, tempVec.y);
                            }
                            else
                            {
                                playerRigidBody.velocity = playerRigidBody.velocity * (1.0f - Time.deltaTime * normalMod);
                            }
                            break;
                        }
                    }

                    tempVec.x = playerRigidBody.transform.position.x;
                    tempVec.y = moveCenter.position.y;
                    tempVec.z = playerRigidBody.transform.position.z;
                    if (Vector3.Distance(tempVec, moveCenter.position) > maxHorizontal)
                    {
                        tempVec = (tempVec - moveCenter.position).normalized * maxHorizontal;
                    }

                    tempVec.y = Mathf.Clamp(playerRigidBody.transform.position.y, minVertical, maxVertical);
                    playerRigidBody.transform.position = tempVec;
                }
            }
        }

        private bool IterativeCollisionSphereCast(Vector3 startPosition, float sphereRadius, Vector3 movementVector, float precision, out Vector3 endPosition, bool singleHand)
        {

            Vector3 movementToProjectedAboveCollisionPlane;
            Surface gorillaSurface;
            float slipPercentage;
            //first spherecast from the starting position to the final position
            if (CollisionsSphereCast(startPosition, sphereRadius * precision, movementVector, precision, out endPosition, out hitInfo))
            {
                //if we hit a surface, do a bit of a slide. this makes it so if you grab with two hands you don't stick 100%, and if you're pushing along a surface while braced with your head, your hand will slide a bit

                //take the surface normal that we hit, then along that plane, do a spherecast to a position a small distance away to account for moving perpendicular to that surface
                Vector3 firstPosition = endPosition;
                gorillaSurface = hitInfo.collider.GetComponent<Surface>();
                slipPercentage = gorillaSurface != null ? gorillaSurface.slipPercentage : (!singleHand ? defaultSlideFactor : 0.001f);
                movementToProjectedAboveCollisionPlane = Vector3.ProjectOnPlane(startPosition + movementVector - firstPosition, hitInfo.normal) * slipPercentage;
                if (CollisionsSphereCast(endPosition, sphereRadius, movementToProjectedAboveCollisionPlane, precision * precision, out endPosition, out hitInfo))
                {
                    //if we hit trying to move perpendicularly, stop there and our end position is the final spot we hit
                    return true;
                }
                //if not, try to move closer towards the true point to account for the fact that the movement along the normal of the hit could have moved you away from the surface
                else if (CollisionsSphereCast(movementToProjectedAboveCollisionPlane + firstPosition, sphereRadius, startPosition + movementVector - (movementToProjectedAboveCollisionPlane + firstPosition), precision * precision * precision, out endPosition, out hitInfo))
                {
                    //if we hit, then return the spot we hit
                    return true;
                }
                else
                {
                    //this shouldn't really happe, since this means that the sliding motion got you around some corner or something and let you get to your final point. back off because something strange happened, so just don't do the slide
                    endPosition = firstPosition;
                    return true;
                }
            }
            //as kind of a sanity check, try a smaller spherecast. this accounts for times when the original spherecast was already touching a surface so it didn't trigger correctly
            else if (CollisionsSphereCast(startPosition, sphereRadius * precision * 0.66f, movementVector.normalized * (movementVector.magnitude + sphereRadius * precision * 0.34f), precision * 0.66f, out endPosition, out hitInfo))
            {
                endPosition = startPosition;
                return true;
            }
            else
            {
                endPosition = Vector3.zero;
                return false;
            }
        }

        private bool CollisionsSphereCast(Vector3 startPosition, float sphereRadius, Vector3 movementVector, float precision, out Vector3 finalPosition, out RaycastHit hitInfo)
        {
            //kind of like a souped up spherecast. includes checks to make sure that the sphere we're using, if it touches a surface, is pushed away the correct distance (the original sphereradius distance). since you might
            //be pushing into sharp corners, this might not always be valid, so that's what the extra checks are for

            //initial spherecase
            RaycastHit innerHit;
            if (Physics.SphereCast(startPosition, sphereRadius * precision, movementVector, out hitInfo, movementVector.magnitude + sphereRadius * (1 - precision), locomotionEnabledLayers.value))
            {
                //if we hit, we're trying to move to a position a sphereradius distance from the normal
                finalPosition = hitInfo.point + hitInfo.normal * sphereRadius;

                //check a spherecase from the original position to the intended final position
                if (Physics.SphereCast(startPosition, sphereRadius * precision * precision, finalPosition - startPosition, out innerHit, (finalPosition - startPosition).magnitude + sphereRadius * (1 - precision * precision), locomotionEnabledLayers.value))
                {
                    finalPosition = startPosition + (finalPosition - startPosition).normalized * Mathf.Max(0, hitInfo.distance - sphereRadius * (1f - precision * precision));
                    hitInfo = innerHit;
                }
                //bonus raycast check to make sure that something odd didn't happen with the spherecast. helps prevent clipping through geometry
                else if (Physics.Raycast(startPosition, finalPosition - startPosition, out innerHit, (finalPosition - startPosition).magnitude + sphereRadius * precision * precision * 0.999f, locomotionEnabledLayers.value))
                {
                    finalPosition = startPosition;
                    hitInfo = innerHit;
                    return true;
                }
                return true;
            }
            //anti-clipping through geometry check
            else if (Physics.Raycast(startPosition, movementVector, out hitInfo, movementVector.magnitude + sphereRadius * precision * 0.999f, locomotionEnabledLayers.value))
            {
                finalPosition = startPosition;
                return true;
            }
            else
            {
                finalPosition = Vector3.zero;
                return false;
            }
        }

        public bool IsHandTouching(bool forLeftHand)
        {
            if (forLeftHand)
            {
                return wasLeftHandTouching;
            }
            else
            {
                return wasRightHandTouching;
            }
        }

        public void Turn(float degrees)
        {
            transform.RotateAround(headCollider.transform.position, transform.up, degrees);
            denormalizedVelocityAverage = Quaternion.Euler(0, degrees, 0) * denormalizedVelocityAverage;
            for (int i = 0; i < velocityHistory.Length; i++)
            {
                velocityHistory[i] = Quaternion.Euler(0, degrees, 0) * velocityHistory[i];
            }
        }

        public void StartVibration(bool forLeftController, float amplitude, float duration)
        {
            base.StartCoroutine(this.HapticPulses(forLeftController, amplitude, duration));
        }

        // Token: 0x06000315 RID: 789 RVA: 0x00016512 File Offset: 0x00014712
        private IEnumerator HapticPulses(bool forLeftController, float amplitude, float duration)
        {
            float startTime = Time.time;
            uint channel = 0U;
            UnityEngine.XR.InputDevice device;
            if (forLeftController)
            {
                device = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            }
            else
            {
                device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            }
            while (Time.time < startTime + duration)
            {
                device.SendHapticImpulse(channel, amplitude, this.hapticWaitSeconds);
                yield return new WaitForSeconds(this.hapticWaitSeconds * 0.9f);
            }
            yield break;
        }

        private void StoreVelocities()
        {
            velocityIndex = (velocityIndex + 1) % velocityHistorySize;
            Vector3 oldestVelocity = velocityHistory[velocityIndex];
            currentVelocity = (useLocal ? transform.localPosition - lastPosition : transform.position - lastPosition + leftMove) / Time.deltaTime;
            denormalizedVelocityAverage += (currentVelocity - oldestVelocity) / (float)velocityHistorySize;
            velocityHistory[velocityIndex] = currentVelocity;
            lastPosition = useLocal ? transform.localPosition : transform.position + leftMove;
        }

        public void DisableActiveColliders()
        {
            for (int i = 0; i < activeColliders.Count; i++)
            {
                activeColliders[i].enabled = false;
            }
        }

        public void EnableActiveColliders()
        {
            for (int i = 0; i < activeColliders.Count; i++)
            {
                activeColliders[i].enabled = true;
            }
        }

        public bool ShouldBroadcastSound()
        {
            return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Yeti" || 
                   UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "PenguinRoyale" || 
                   UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "PenguinRoyaleFFA" || 
                   UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "PenguinRoyaleDuos" || 
                   UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "PenguinRoyaleSquads" || 
                   UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "PenguinRoyaleChaos" || 
                   UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "CubeWars";
        }

        public void BroadcastSound(int type)
        {
            if (ShouldBroadcastSound())
            {
                if (myPlayer == null)
                {
                    PhotonVRPlayer[] players = FindObjectsOfType<PhotonVRPlayer>();
                    for (int i = 0; i < players.Length; i++)
                    {
                        if (players[i].photonView.IsMine)
                        {
                            myPlayer = players[i];
                            break;
                        }
                    }
                }
                myPlayer.photonView.RPC("PlayFootstep", Photon.Pun.RpcTarget.Others, type);
            }
        }

        public void PlayFootstepSound(PhotonVRPlayer.SoundType type, Vector3 position)
        {
            if (myPlayer == null)
            {
                PhotonVRPlayer[] players = FindObjectsOfType<PhotonVRPlayer>();
                for (int i = 0; i < players.Length; i++)
                {
                    if (players[i].photonView.IsMine)
                    {
                        myPlayer = players[i];
                        break;
                    }
                }
            }

            lastSource = (lastSource + 1) % publicSoundSources.Length;
            AudioSource source = publicSoundSources[lastSource];
            source.Stop();
            source.transform.position = position;

            switch (type)
            {
                case PhotonVRPlayer.SoundType.Meat:
                {
                    source.clip = MeatHitSound.clip;
                    break;
                }

                case PhotonVRPlayer.SoundType.Metal:
                {
                    source.clip = MetalHitSound.clip;
                    break;
                }

                case PhotonVRPlayer.SoundType.Rock:
                {
                    source.clip = RockHitSound.clip;
                    break;
                }

                case PhotonVRPlayer.SoundType.Snow:
                {
                    source.clip = SnowHitSound.clip;
                    break;
                }

                case PhotonVRPlayer.SoundType.Wood:
                {
                    source.clip = WoodHitSound.clip;
                    break;
                }
            }
            source.Play();
        }

        public void ForceMovePlayerToPosition(Vector3 position, bool instant = false)
        {
            transform.position = position;
            InitializeValues();
        }

        public void OnDrawGizmosSelected()
        {
            if (moveCenter != null)
            {
                Gizmos.DrawWireCube(new Vector3(moveCenter.position.x, (minVertical + maxVertical) / 2.0f, moveCenter.position.z), 
                                    new Vector3(maxHorizontal, (maxVertical - minVertical), maxHorizontal));
            }
        }
    }
}
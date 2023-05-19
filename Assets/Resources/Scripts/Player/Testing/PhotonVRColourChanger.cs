using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Photon.VR.Player.Testing
{
    public class PhotonVRColourChanger : MonoBehaviour
    {
        public void ChangeColour(Color Colour) => PhotonVRManager.SetColour(Colour);
    }
}
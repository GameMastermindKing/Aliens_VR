using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

//Made By Fireblast
public class PlayFabManager : MonoBehaviour
{
    public GameObject computer;
    public static PlayFabManager instance;
    public List<GameObject> Cosmetics;
    public List<GameObject> CosmeticsDiss;
    public List<GameObject> errorMessages;
    [HideInInspector] public bool hasHammer;
    public string MyPlayFabID;
    public bool loggedIn = false;
    public bool logging = false;

    #if UNITY_EDITOR
    public bool forceFailure = false;
    #endif

    private void Awake()
    {
        instance = this;
    }
    
    void Start()
    {
        /*if(PenguinCryptography.Decrypt("Banned", "Banned") == "Banned")
        {
            SceneManager.LoadScene("Gorilla Locomotion 1");
        }*/
        Login();
    }

    public void Login()
    {
        if (!logging)
        {
            #if UNITY_EDITOR
            if (forceFailure)
            {
                PlayFabError testError = new PlayFabError();
                testError.Error = PlayFabErrorCode.ConnectionError;
                OnError(testError);
                return;
            }
            #endif

            logging = true;
            var request = new LoginWithCustomIDRequest
            {
                CustomId = SystemInfo.deviceUniqueIdentifier,
                CreateAccount = true
            };
            PlayFabClientAPI.LoginWithCustomID(request, OnSuccess, OnError);
        }
    }

    void OnSuccess(LoginResult result)
    {
        string pUsername = PlayerPrefs.GetString("username");
        PlayFabClientAPI.UpdateUserTitleDisplayName(new UpdateUserTitleDisplayNameRequest
        {
            DisplayName = pUsername
        }, delegate (UpdateUserTitleDisplayNameResult result)
        {
            Debug.Log("Playfab Name Changed!");
        }, delegate (PlayFabError error)
        {
            Debug.Log("PLAYFAB ERROR: " + error.ErrorDetails);
        });
        GetAccountInfoRequest InfoRequest = new GetAccountInfoRequest();
        PlayFabClientAPI.GetAccountInfo(InfoRequest, AccountInfoSuccess, OnError);
        computer.SetActive(true);
        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), delegate (GetUserInventoryResult result)
        {
            logging = false;
            loggedIn = true;
            for (int i = 0; i < errorMessages.Count; i++)
            {
                errorMessages[i].SetActive(false);
            }

            for (int i = 0; i < CosmeticsDiss.Count; i++)
            {
                CosmeticsDiss[i].SetActive(true);
            }

            foreach (ItemInstance item in result.Inventory)
            {
                if (item.CatalogVersion == "Cosmetics")
                {
                    for (int i = 0; i < Cosmetics.Count; i++)
                    {
                        if (Cosmetics[i].name == item.ItemId)
                        {
                            Cosmetics[i].SetActive(true);
                        }
                        /* else
                         {
                             Cosmetics[i].SetActive(false);
                         }*/
                    }
                    for (int i = 0; i < CosmeticsDiss.Count; i++)
                    {
                        if (CosmeticsDiss[i].name == item.ItemId)
                        {
                            CosmeticsDiss[i].SetActive(false);
                        }
                    }

                    if ("BanHammer" == item.ItemId)
                    {
                        hasHammer = true;
                        Debug.Log("PLAYER OWNS HAMMER");
                    }
                    else
                    {
                        hasHammer = false;
                    }
                }
            }
        }, delegate (PlayFabError error)
        {
            OnError(error);
        });
    }

    public void AccountInfoSuccess(GetAccountInfoResult result)
    {
        MyPlayFabID = result.AccountInfo.PlayFabId;
    }

    void OnError(PlayFabError error)
    {
        logging = false;
        loggedIn = false;
        if (error.Error == PlayFabErrorCode.AccountBanned)
        {
            //PenguinCryptography.Encrypt("Banned", "Banned");
            Debug.Log("PLAYER IS BANNED");
            SceneManager.LoadScene("Gorilla Locomotion 1");
        }
        else
        {   
            for (int i = 0; i < errorMessages.Count; i++)
            {
                errorMessages[i].SetActive(true);
            }

            for (int i = 0; i < Cosmetics.Count; i++)
            {
                Cosmetics[i].SetActive(false);
            }

            for (int i = 0; i < CosmeticsDiss.Count; i++)
            {
                CosmeticsDiss[i].SetActive(false);
            }
        }

        #if !UNITY_EDITOR
        Debug.Log(error.GenerateErrorReport());
        #endif
    }

    public virtual bool PlayFabPlayerLoggedIn()
    {
        return loggedIn;
    }
}
/*public static class PenguinCryptography
{
    private const int Keysize = 256;

    // This constant determines the number of iterations for the password bytes generation function.
    private const int DerivationIterations = 1000;

    public static string Encrypt(string plainText, string passPhrase)
    {
        // Salt and IV is randomly generated each time, but is preprended to encrypted cipher text
        // so that the same Salt and IV values can be used when decrypting.  
        var saltStringBytes = Generate256BitsOfRandomEntropy();
        var ivStringBytes = Generate256BitsOfRandomEntropy();
        var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
        using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
        {
            var keyBytes = password.GetBytes(Keysize / 8);
            using (var symmetricKey = new RijndaelManaged())
            {
                symmetricKey.BlockSize = 256;
                symmetricKey.Mode = CipherMode.CBC;
                symmetricKey.Padding = PaddingMode.PKCS7;
                using (var encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes))
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                        {
                            cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                            cryptoStream.FlushFinalBlock();
                            // Create the final bytes as a concatenation of the random salt bytes, the random iv bytes and the cipher bytes.
                            var cipherTextBytes = saltStringBytes;
                            cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
                            cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();
                            memoryStream.Close();
                            cryptoStream.Close();
                            return Convert.ToBase64String(cipherTextBytes);
                        }
                    }
                }
            }
        }
    }

    public static string Decrypt(string cipherText, string passPhrase)
    {
        // Get the complete stream of bytes that represent:
        // [32 bytes of Salt] + [32 bytes of IV] + [n bytes of CipherText]
        var cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
        // Get the saltbytes by extracting the first 32 bytes from the supplied cipherText bytes.
        var saltStringBytes = cipherTextBytesWithSaltAndIv.Take(Keysize / 8).ToArray();
        // Get the IV bytes by extracting the next 32 bytes from the supplied cipherText bytes.
        var ivStringBytes = cipherTextBytesWithSaltAndIv.Skip(Keysize / 8).Take(Keysize / 8).ToArray();
        // Get the actual cipher text bytes by removing the first 64 bytes from the cipherText string.
        var cipherTextBytes = cipherTextBytesWithSaltAndIv.Skip((Keysize / 8) * 2).Take(cipherTextBytesWithSaltAndIv.Length - ((Keysize / 8) * 2)).ToArray();

        using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
        {
            var keyBytes = password.GetBytes(Keysize / 8);
            using (var symmetricKey = new RijndaelManaged())
            {
                symmetricKey.BlockSize = 256;
                symmetricKey.Mode = CipherMode.CBC;
                symmetricKey.Padding = PaddingMode.PKCS7;
                using (var decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes))
                {
                    using (var memoryStream = new MemoryStream(cipherTextBytes))
                    {
                        using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                        using (var streamReader = new StreamReader(cryptoStream, Encoding.UTF8))
                        {
                            return streamReader.ReadToEnd();
                        }
                    }
                }
            }
        }
    }

    private static byte[] Generate256BitsOfRandomEntropy()
    {
        var randomBytes = new byte[32]; // 32 Bytes will give us 256 bits.
        using (var rngCsp = new RNGCryptoServiceProvider())
        {
            // Fill the array with cryptographically secure random bytes.
            rngCsp.GetBytes(randomBytes);
        }
        return randomBytes;
    }
}*/
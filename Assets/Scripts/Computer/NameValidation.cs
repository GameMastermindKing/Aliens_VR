using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NameValidation : MonoBehaviour
{
    [System.Serializable]
    public class BadNameData
    {
        public string fullBadName;
        public bool allowSymbols = false;
        public int maxSimilarChars = 2;
    }

    [System.Serializable]
    public class BadVowelReplacement
    {
        public char vowel;
        public List<char> badReplacements;
    }

    [System.Serializable]
    public class BadConsonantReplacement
    {
        public char consonant;
        public List<char> badReplacements;
    }

    public List<string> babyNames;
    public List<BadVowelReplacement> badVowelReplacements;
    public List<BadConsonantReplacement> badConsonantReplacements;
    public List<BadNameData> badNameDatas;
    public List<string> whitelist;
    public List<char> allowedCharacters;

    #if UNITY_EDITOR
    public string badNameTest;
    public bool testNow;
    #endif

    BadVowelReplacement tempVowelReplacement;
    BadConsonantReplacement tempConsonantReplacement;

    public static NameValidation instance;

    public void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #if UNITY_EDITOR
    public void Update()
    {
        if (testNow)
        {
            testNow = false;
            Debug.Log(IsNameValid(badNameTest));
        }
    }
    #endif

    public bool IsNameValid(string name, bool ignoreLength = false)
    {
        if ((!ignoreLength && (name.Length > 15 || name.Length < 1)) || (ignoreLength && name.Length < 1))
        {
            Debug.Log("Length");
            return false;
        }

        if (NameHasSymbols(name))
        {
            Debug.Log("Symbols");
            return false;
        }

        for (int i = 0; i < whitelist.Count; i++)
        {
            if (name.ToLower() == whitelist[i].ToLower())
            {
                return true;
            }
        }

        for (int i = 0; i < badNameDatas.Count; i++)
        {
            if (IsAdjacentTo(badNameDatas[i], name))
            {
                Debug.Log("AdjacentTo " + badNameDatas[i].fullBadName);
                return false;
            }
        }
        return true;
    }

    public bool IsAdjacentTo(BadNameData data, string name)
    {
        string badName = data.fullBadName.ToLower();
        string lowerName = name.ToLower();

        if (lowerName.Contains(badName))
        {
            return true;
        }

        if (badName.Length < lowerName.Length)
        {
            return false;
        }

        if (badName == lowerName)
        {
            return true;
        }

        int numSimilar = 0;
        for (int i = 0; i < lowerName.Length; i++)
        {
            if (badName[i] == lowerName[i])
            {
                numSimilar++;
            }
            else if (IsVowel(badName[i]))
            {
                tempVowelReplacement = GetVowelReplacement(badName[i]);
                if (tempVowelReplacement != null)
                {
                    if (tempVowelReplacement.badReplacements.Contains(lowerName[i]))
                    {
                        numSimilar++;
                    }
                }
            }
            else
            {
                tempConsonantReplacement = GetConsonantReplacement(badName[i]);
                if (tempConsonantReplacement != null)
                {
                    if (tempConsonantReplacement.badReplacements.Contains(lowerName[i]))
                    {
                        numSimilar++;
                    }
                }
            }
        }
        return numSimilar > data.maxSimilarChars; 
    }

    public BadConsonantReplacement GetConsonantReplacement(char consonant)
    {
        for (int i = 0; i < badConsonantReplacements.Count; i++)
        {
            if (badConsonantReplacements[i].consonant == consonant)
            {
                return badConsonantReplacements[i];
            }
        }
        return null;
    }

    public BadVowelReplacement GetVowelReplacement(char vowel)
    {
        for (int i = 0; i < badConsonantReplacements.Count; i++)
        {
            if (badVowelReplacements[i].vowel == vowel)
            {
                return badVowelReplacements[i];
            }
        }
        return null;
    }

    public bool IsVowel(char charToCheck)
    {
        return charToCheck == 'A' || charToCheck == 'E' || charToCheck == 'I' || charToCheck == 'O' || charToCheck == 'U' || 
               charToCheck == 'a' || charToCheck == 'e' || charToCheck == 'i' || charToCheck == 'o' || charToCheck == 'u';
    }

    public string GetFixName()
    {
        return babyNames[Random.Range(0, babyNames.Count)] + Random.Range(0, 10000000).ToString("0000000");
    }

    public bool NameHasSymbols(string name)
    {
        for (int i = 0; i < name.Length; i++)
        {
            if (!allowedCharacters.Contains(name[i]))
            {
                return true;
            }
        }
        return false;
    }
}

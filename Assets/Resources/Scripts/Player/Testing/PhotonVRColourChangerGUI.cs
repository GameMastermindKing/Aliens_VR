#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

using Photon.Pun;

namespace Photon.VR.Player.Testing
{
    [CustomEditor(typeof(PhotonVRColourChanger))]
    public class PhotonVRColourChangerGUI : Editor
    {
        private Color Colour;
        public override void OnInspectorGUI()
        {
            if (PhotonNetwork.InRoom)
            {
                PhotonVRColourChanger manager = (PhotonVRColourChanger)target;
                Colour = EditorGUILayout.ColorField(Colour);
                if (GUILayout.Button("Change"))
                    manager.ChangeColour(Colour);
            }
            else
            {
                GUILayout.Label("Not in a room");
            }
        }
    }
}
#endif
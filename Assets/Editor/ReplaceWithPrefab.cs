using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class ReplaceWithPrefab : EditorWindow
{
    [SerializeField] private GameObject prefab;

    [MenuItem("Tools/Replace With Prefab")]
    static void CreateReplaceWithPrefab()
    {
        EditorWindow.GetWindow<ReplaceWithPrefab>();
    }

    private void OnGUI()
    {
        prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), false);

        if (GUILayout.Button("Replace"))
        {
            var selection = Selection.gameObjects;

            for (var i = selection.Length - 1; i >= 0; --i)
            {
                var selected = selection[i];
                GameObject newObject;

                newObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                newObject.name = selected.name;

                if (newObject == null)
                {
                    Debug.LogError("Error instantiating prefab");
                    break;
                }

                Text textComponent = selected.GetComponent<Text>();
                Text newText = newObject.GetComponent<Text>();

                RectTransform oldTransform = selected.GetComponent<RectTransform>();
                RectTransform newTransform = newObject.GetComponent<RectTransform>();

                Undo.RegisterCreatedObjectUndo(newObject, "Replace With Prefabs");
                newTransform.SetParent(oldTransform.parent, false);
                newTransform.localPosition = oldTransform.localPosition;
                newTransform.localRotation = oldTransform.localRotation;
                newTransform.localScale = oldTransform.localScale;
                newTransform.SetSiblingIndex(oldTransform.GetSiblingIndex());
                newTransform.anchoredPosition = oldTransform.anchoredPosition;
                newTransform.anchorMax = oldTransform.anchorMax;
                newTransform.anchorMin = oldTransform.anchorMin;
                newTransform.pivot = oldTransform.pivot;
                newTransform.sizeDelta = oldTransform.sizeDelta;
                newTransform.offsetMax = oldTransform.offsetMax;
                newTransform.offsetMin = oldTransform.offsetMin;

                newText.text = textComponent.text;
                newText.fontSize = textComponent.fontSize;
                newText.fontStyle = textComponent.fontStyle;
                newText.alignment = textComponent.alignment;
                newText.color = textComponent.color;                

                Undo.DestroyObjectImmediate(selected);
            }
        }

        GUI.enabled = false;
        EditorGUILayout.LabelField("Selection count: " + Selection.objects.Length);
    }
}

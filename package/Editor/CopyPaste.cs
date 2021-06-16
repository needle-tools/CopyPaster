using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public class CopyPaste
{
	private static bool DebugLog
	{
		get => SessionState.GetBool("CopyPasteDebugLog", false);
		set => SessionState.SetBool("CopyPasteDebugLog", value);
	} 

	[InitializeOnLoadMethod]
	private static void Init()
	{
		EditorApplication.update += FindSceneHierarchyWindow;
		// EditorApplication.update += () => Debug.Log(EditorGUIUtility.systemCopyBuffer);
	}

	private static void FindSceneHierarchyWindow()
	{
		var window = Resources.FindObjectsOfTypeAll<SceneHierarchyWindow>().FirstOrDefault(w => w);
		if (window && window.rootVisualElement != null)
		{
			var container = window.rootVisualElement.parent.Query<IMGUIContainer>().First();
			if (container != null)
			{
				container.onGUIHandler += () => OnHierarchyGUI(window);
				EditorApplication.update -= FindSceneHierarchyWindow;
			}
		}
	}

	private static void OnHierarchyGUI(EditorWindow window)
	{
		if (window && window.hasFocus)
		{
			if (Event.current.commandName == "Copy")
			{
				ExecuteCopy();
			}
			else if (Event.current.commandName == "Paste")
			{
				ExecutePaste();
			}
		}
	}

	private static void ExecuteCopy()
	{
		var obj = Selection.activeGameObject;
		if (!obj) return;
		
		var hierarchy = new List<Object>();
		void CollectRecursive(GameObject go)
		{
			hierarchy.Add(go);
			hierarchy.AddRange(go.GetComponents<Component>());
			foreach (var ch in go.transform)
			{
				var t = ch as Transform;
				if (t)
					// ReSharper disable once PossibleNullReferenceException
					CollectRecursive(t.gameObject);
			}
		}

		CollectRecursive(obj);
		
		if(DebugLog) Debug.Log("COPY " + obj, obj);
		var path = Application.dataPath + "/../Temp/CopyBuffer.asset";
		InternalEditorUtility.SaveToSerializedFileAndForget(hierarchy.ToArray(), path, true);
		EditorGUIUtility.systemCopyBuffer = path;
	}

	private static string CurrentCopyBuffer
	{
		get => EditorGUIUtility.systemCopyBuffer;
		set => EditorGUIUtility.systemCopyBuffer = value;
	}
	
	private static string LocalCopyBufferPath => Application.dataPath + "/../Temp/CopyBuffer.asset";
	private static bool CopyBufferIsInOwnProject => EditorGUIUtility.systemCopyBuffer?.StartsWith(Application.dataPath) ?? false;

	private static void ExecutePaste()
	{
		if (CurrentCopyBuffer == null || !CurrentCopyBuffer.EndsWith("CopyBuffer.asset")) return;
		var path = CurrentCopyBuffer;
		if (!File.Exists(path)) return;
		
#if UNITY_2020_1_OR_NEWER
		Unsupported.ClearPasteboard();
		// if (Unsupported.CanPasteGameObjectsFromPasteboard())
		// {
		// }
#endif
		
		if(DebugLog) Debug.Log("PASTE " + CurrentCopyBuffer);
		var objs = InternalEditorUtility.LoadSerializedFileAndForget(CurrentCopyBuffer);
		CurrentCopyBuffer = null;
		foreach (var obj in objs)
		{
			if (obj is GameObject go)
			{
				var instance = Object.Instantiate(go);
				instance.name = go.name;
				Selection.activeObject = instance;
				break;
			}
		}
	}
}
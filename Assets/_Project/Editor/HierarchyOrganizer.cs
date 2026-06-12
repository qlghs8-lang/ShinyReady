using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class HierarchyOrganizer
{
    private static readonly Dictionary<string, string[]> Groups = new()
    {
        {
            "[--- System ---]",
            new[] { "Main Camera", "Directional Light", "Global Volume", "EventSystem" }
        },
        {
            "[--- UI ---]",
            new[] { "JoyStickCanvas", "UpgradeCanvas", "UnlockZonePopupCanvas", "OfflineRewardPopupCanvas" }
        },
        {
            "[--- Managers ---]",
            new[]
            {
                "CurrencyManager", "GameSaveManager", "SoundManager",
                "UpgradeManager", "SoapInventoryManager", "AdBuffManager", "OfflineRewardManager"
            }
        },
        {
            "[--- Player ---]",
            new[] { "Player" }
        },
        {
            "[--- Gameplay ---]",
            new[] { "SoapWarehouse", "Office", "DetailingZone", "UnlockZone_01", "UnlockZone_02" }
        },
        {
            "[--- Waypoints ---]",
            new[] { "CarWaypoints_01", "CarWaypoints_02" }
        },
        {
            "[--- Environment ---]",
            new[]
            {
                "Plane", "Wall",
                "Road_Washbay_01", "Road_Washbay_02", "Road_DetailingZone", "Road_Grass",
                "Base_Washbay_01", "Base_Washbay_02", "Base_DetailingZone"
            }
        },
    };

    [MenuItem("Tools/Shiny Ready/Organize Scene Hierarchy")]
    public static void OrganizeHierarchy()
    {
        // 비활성 오브젝트 포함 전체 씬 오브젝트 이름 맵 구성
        var nameMap = BuildNameMap();

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Organize Scene Hierarchy");
        int undoGroup = Undo.GetCurrentGroup();

        foreach (var kvp in Groups)
        {
            string groupName = kvp.Key;
            string[] childNames = kvp.Value;

            // 그룹 오브젝트도 비활성일 수 있으므로 nameMap에서 탐색
            GameObject groupObj = nameMap.TryGetValue(groupName, out var found) ? found : null;
            if (groupObj == null)
            {
                groupObj = new GameObject(groupName);
                Undo.RegisterCreatedObjectUndo(groupObj, $"Create {groupName}");
                groupObj.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                groupObj.transform.localScale = Vector3.one;
                nameMap[groupName] = groupObj;
            }

            foreach (string childName in childNames)
            {
                if (nameMap.TryGetValue(childName, out GameObject child))
                {
                    // 이미 올바른 부모 아래 있으면 스킵
                    if (child.transform.parent == groupObj.transform) continue;
                    Undo.SetTransformParent(child.transform, groupObj.transform, $"Move {childName}");
                }
                else
                {
                    Debug.LogWarning($"[HierarchyOrganizer] '{childName}' not found — skipped.");
                }
            }
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[HierarchyOrganizer] Done! Ctrl+Z to undo all at once.");
    }

    // 비활성 포함 씬 전체 GameObject를 이름으로 탐색 (중복 시 첫 번째 사용)
    private static Dictionary<string, GameObject> BuildNameMap()
    {
        var map = new Dictionary<string, GameObject>();
        var allTransforms = Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var t in allTransforms)
        {
            if (!map.ContainsKey(t.gameObject.name))
                map[t.gameObject.name] = t.gameObject;
        }
        return map;
    }
}

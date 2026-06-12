using System.IO;
using UnityEditor;
using UnityEngine;

public static class ProjectOrganizer
{
    private const string Root = "Assets/_Project";

    [MenuItem("Tools/Shiny Ready/Organize Project Folders")]
    public static void OrganizeProjectFolders()
    {
        // --- 폴더 생성 ---
        EnsureFolder($"{Root}/Prefabs");
        EnsureFolder($"{Root}/Prefabs/Currency");
        EnsureFolder($"{Root}/Prefabs/Cars");
        EnsureFolder($"{Root}/Prefabs/Waypoints");
        EnsureFolder($"{Root}/Prefabs/Environment");
        EnsureFolder($"{Root}/Prefabs/UI");
        EnsureFolder($"{Root}/Fonts");
        EnsureFolder($"{Root}/Textures/UI");

        // --- Currency 프리팹 ---
        Move($"{Root}/Scripts/Currency/Cash.prefab",        $"{Root}/Prefabs/Currency/Cash.prefab");
        Move($"{Root}/Scripts/Currency/Coin.prefab",        $"{Root}/Prefabs/Currency/Coin.prefab");
        Move($"{Root}/Scripts/Currency/Gold Ingot.prefab",  $"{Root}/Prefabs/Currency/Gold Ingot.prefab");
        Move($"{Root}/Scripts/Currency/MoneyPickup.prefab", $"{Root}/Prefabs/Currency/MoneyPickup.prefab");

        // --- Car 프리팹 ---
        Move($"{Root}/Scripts/_Car/CarPrefab.prefab",  $"{Root}/Prefabs/Cars/CarPrefab.prefab");
        Move($"{Root}/Scripts/_Car/Hatchback.prefab",  $"{Root}/Prefabs/Cars/Hatchback.prefab");
        Move($"{Root}/Scripts/_Car/Pickup.prefab",     $"{Root}/Prefabs/Cars/Pickup.prefab");
        Move($"{Root}/Scripts/_Car/Police.prefab",     $"{Root}/Prefabs/Cars/Police.prefab");
        Move($"{Root}/Scripts/_Car/Taxi.prefab",       $"{Root}/Prefabs/Cars/Taxi.prefab");
        Move($"{Root}/Scripts/_Car/Towtruck.prefab",   $"{Root}/Prefabs/Cars/Towtruck.prefab");
        Move($"{Root}/Scripts/_Car/Truck.prefab",      $"{Root}/Prefabs/Cars/Truck.prefab");
        Move($"{Root}/Scripts/_Car/Van.prefab",        $"{Root}/Prefabs/Cars/Van.prefab");
        Move($"{Root}/Scripts/_Car/VanBig.prefab",     $"{Root}/Prefabs/Cars/VanBig.prefab");

        // --- Waypoints 프리팹 ---
        Move($"{Root}/Scripts/_Car/CarWaypoints_02.prefab", $"{Root}/Prefabs/Waypoints/CarWaypoints_02.prefab");

        // --- Environment 프리팹 ---
        Move($"{Root}/Scripts/UI/Base_DetailingZone.prefab", $"{Root}/Prefabs/Environment/Base_DetailingZone.prefab");
        Move($"{Root}/Scripts/UI/Base_Washbay_02.prefab",    $"{Root}/Prefabs/Environment/Base_Washbay_02.prefab");
        Move($"{Root}/Scripts/UI/Road_DetailingZone.prefab", $"{Root}/Prefabs/Environment/Road_DetailingZone.prefab");
        Move($"{Root}/Scripts/UI/Road_Grass.prefab",         $"{Root}/Prefabs/Environment/Road_Grass.prefab");
        Move($"{Root}/Scripts/UI/Road_Washbay_01.prefab",    $"{Root}/Prefabs/Environment/Road_Washbay_01.prefab");
        Move($"{Root}/Scripts/UI/Road_Washbay_02.prefab",    $"{Root}/Prefabs/Environment/Road_Washbay_02.prefab");
        Move($"{Root}/Scripts/UI/SoapWareHouse01.prefab",    $"{Root}/Prefabs/Environment/SoapWareHouse01.prefab");
        Move($"{Root}/Scripts/UI/Office01.prefab",           $"{Root}/Prefabs/Environment/Office01.prefab");

        // --- UI 프리팹 ---
        Move($"{Root}/Scripts/UI/SoapShopItem.prefab", $"{Root}/Prefabs/UI/SoapShopItem.prefab");

        // --- 폰트 ---
        Move($"{Root}/Scripts/UI/Fonts/BerkshireSwash-Regular.ttf", $"{Root}/Fonts/BerkshireSwash-Regular.ttf");
        Move($"{Root}/Scripts/UI/Fonts/Candara.ttf",  $"{Root}/Fonts/Candara.ttf");
        Move($"{Root}/Scripts/UI/Fonts/Candarab.ttf", $"{Root}/Fonts/Candarab.ttf");
        Move($"{Root}/Scripts/UI/Fonts/Candarai.ttf", $"{Root}/Fonts/Candarai.ttf");
        Move($"{Root}/Scripts/UI/Fonts/Candaraz.ttf", $"{Root}/Fonts/Candaraz.ttf");

        // --- GUI Elements 폴더 통째로 이동 ---
        Move($"{Root}/Scripts/UI/GUI Elements", $"{Root}/Textures/UI/GUI Elements");

        // --- 루트에 떠 있는 SoapData.asset ---
        Move($"{Root}/SoapData.asset", $"{Root}/ScriptableObjects/SoapData/SoapData.asset");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ProjectOrganizer] 완료! Console 경고 메시지를 확인하세요.");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string folderName = Path.GetFileName(path);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }

    private static void Move(string from, string to)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(from) == null)
        {
            Debug.LogWarning($"[ProjectOrganizer] 없음(스킵): {from}");
            return;
        }

        string error = AssetDatabase.MoveAsset(from, to);
        if (!string.IsNullOrEmpty(error))
            Debug.LogError($"[ProjectOrganizer] 이동 실패 '{from}' → '{to}': {error}");
    }
}

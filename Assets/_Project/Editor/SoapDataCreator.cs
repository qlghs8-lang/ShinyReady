using UnityEngine;
using UnityEditor;
using ShinyReady.Cleaning;

namespace ShinyReady.Editor
{
    /// <summary>
    /// 메뉴: ShinyReady > Create Soap Data Assets
    /// Assets/_Project/ScriptableObjects/Soap/ 폴더에 4종 SoapData 에셋을 생성한다.
    /// 이미 존재하는 파일은 덮어쓰지 않는다.
    /// </summary>
    public static class SoapDataCreator
    {
        private const string OUTPUT_PATH = "Assets/_Project/ScriptableObjects/Soap";

        [MenuItem("ShinyReady/Create Soap Data Assets")]
        public static void CreateAll()
        {
            if (!AssetDatabase.IsValidFolder(OUTPUT_PATH))
            {
                System.IO.Directory.CreateDirectory(OUTPUT_PATH);
                AssetDatabase.Refresh();
            }

            CreateAsset("BasicSoap", soap =>
            {
                soap.soapName             = "Basic Soap";
                soap.washSpeedMultiplier  = 1.0f;
                soap.incomeMultiplier     = 1.0f;
                soap.detailingChanceBonus = 0f;
                soap.unlockCost           = 0;
                soap.soapAmountPerBox     = 1;
                soap.boxColor             = new Color(0.30f, 0.70f, 1.00f);
            });

            CreateAsset("PowerSoap", soap =>
            {
                soap.soapName             = "Power Soap";
                soap.washSpeedMultiplier  = 1.3f;
                soap.incomeMultiplier     = 1.0f;
                soap.detailingChanceBonus = 0.05f;
                soap.unlockCost           = 500;
                soap.soapAmountPerBox     = 1;
                soap.boxColor             = new Color(0.20f, 0.90f, 0.40f);
            });

            CreateAsset("PremiumWaxSoap", soap =>
            {
                soap.soapName             = "Premium Wax Soap";
                soap.washSpeedMultiplier  = 1.5f;
                soap.incomeMultiplier     = 1.25f;
                soap.detailingChanceBonus = 0.10f;
                soap.unlockCost           = 2000;
                soap.soapAmountPerBox     = 1;
                soap.boxColor             = new Color(1.00f, 0.75f, 0.20f);
            });

            CreateAsset("GoldenSoap", soap =>
            {
                soap.soapName             = "Golden Soap";
                soap.washSpeedMultiplier  = 2.0f;
                soap.incomeMultiplier     = 1.5f;
                soap.detailingChanceBonus = 0.20f;
                soap.unlockCost           = 8000;
                soap.soapAmountPerBox     = 1;
                soap.boxColor             = new Color(1.00f, 0.85f, 0.00f);
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SoapDataCreator] 4종 SoapData 에셋 생성 완료 → {OUTPUT_PATH}");
        }

        private static void CreateAsset(string fileName, System.Action<SoapData> configure)
        {
            string path = $"{OUTPUT_PATH}/{fileName}.asset";
            if (AssetDatabase.LoadAssetAtPath<SoapData>(path) != null)
            {
                Debug.Log($"[SoapDataCreator] 이미 존재해 건너뜀: {path}");
                return;
            }

            var asset = ScriptableObject.CreateInstance<SoapData>();
            configure(asset);
            AssetDatabase.CreateAsset(asset, path);
        }
    }
}

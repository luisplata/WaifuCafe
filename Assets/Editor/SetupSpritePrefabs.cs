using UnityEditor;
using UnityEngine;
using Customers;
using Staff;
using V2.Customer;

public static class SetupSpritePrefabs
{
    private const string CustomerSpritePath = "Assets/Prefabs/Customers/Sprite/CustomerSprite.prefab";
    private const string CustomerUiPath = "Assets/Prefabs/Customers/Ui/Customer.prefab";

    private const string StaffSpritePath = "Assets/Prefabs/Staff/StaffSprite.prefab";
    private const string StaffUiPath = "Assets/Prefabs/Staff/Ui/Staff.prefab";

    [MenuItem("Tools/Setup Sprite Prefabs")]
    public static void SetupAll()
    {
        SetupCustomerSprite();
        SetupStaffSprite();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("✅ Sprite prefabs setup complete!");
    }

    private static void SetupCustomerSprite()
    {
        var parentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CustomerSpritePath);
        var childPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CustomerUiPath);

        if (parentPrefab == null || childPrefab == null)
        {
            Debug.LogError("CustomerSprite or Customer UI prefab not found!");
            return;
        }

        // Open the prefab for editing
        var parentPath = AssetDatabase.GetAssetPath(parentPrefab);
        var parentRoot = PrefabUtility.LoadPrefabContents(parentPath);

        // Add Ui/Customer as a child
        var childInstance = (GameObject)PrefabUtility.InstantiatePrefab((Object)childPrefab);
        if (childInstance != null)
        {
            childInstance.transform.SetParent(parentRoot.transform, false);
            childInstance.name = "CustomerUI";

            var childRect = childInstance.GetComponent<RectTransform>();
            if (childRect != null)
            {
                childRect.anchoredPosition = Vector2.zero;
                childRect.anchorMin = Vector2.zero;
                childRect.anchorMax = Vector2.one;
                childRect.sizeDelta = Vector2.zero;
            }

            // Find CustomerFront and assign the CustomerComponentUi reference
            var customerFront = parentRoot.GetComponent<CustomerSeat>();
            var customerComponentUi = childInstance.GetComponentInChildren<CustomerComponentUi>();

            if (customerFront != null && customerComponentUi != null)
            {
                var serialized = new SerializedObject(customerFront);
                var prop = serialized.FindProperty("customerComponentUi");
                if (prop != null)
                {
                    prop.objectReferenceValue = customerComponentUi;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        // Save changes
        PrefabUtility.SaveAsPrefabAsset(parentRoot, CustomerSpritePath);
        PrefabUtility.UnloadPrefabContents(parentRoot);

        Debug.Log("✅ CustomerSprite configured");
    }

    private static void SetupStaffSprite()
    {
        var parentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StaffSpritePath);
        var childPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StaffUiPath);

        if (parentPrefab == null)
        {
            Debug.LogError("StaffSprite prefab not found!");
            return;
        }

        // Open the prefab for editing
        var parentPath = AssetDatabase.GetAssetPath(parentPrefab);
        var parentRoot = PrefabUtility.LoadPrefabContents(parentPath);

        // Add StaffFront component if not present
        if (parentRoot.GetComponent<StaffFront>() == null)
        {
            parentRoot.AddComponent<StaffFront>();
        }

        // Add Ui/Staff as a child
        if (childPrefab != null)
        {
            var childInstance = (GameObject)PrefabUtility.InstantiatePrefab((Object)childPrefab);
            if (childInstance != null)
            {
                childInstance.transform.SetParent(parentRoot.transform, false);
                childInstance.name = "StaffUI";

                var childRect = childInstance.GetComponent<RectTransform>();
                if (childRect != null)
                {
                    childRect.anchoredPosition = Vector2.zero;
                    childRect.anchorMin = Vector2.zero;
                    childRect.anchorMax = Vector2.one;
                    childRect.sizeDelta = Vector2.zero;
                }
            }
        }

        // Save changes
        PrefabUtility.SaveAsPrefabAsset(parentRoot, StaffSpritePath);
        PrefabUtility.UnloadPrefabContents(parentRoot);

        Debug.Log("✅ StaffSprite configured");
    }

    [MenuItem("Tools/Create Sample Customer Prefabs")]
    public static void CreateSampleCustomers()
    {
        var customerSpritePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CustomerSpritePath);
        if (customerSpritePrefab == null)
        {
            Debug.LogError("CustomerSprite.prefab not found!");
            return;
        }

        // Create 3 sample customer prefabs with different stats (like the old Customer (1-3))
        var samples = new (string name, int patience, int reward)[]
        {
            ("Customer (1)", 60, 10),
            ("Customer (2)", 30, 20),
            ("Customer (3)", 90, 5)
        };

        foreach (var (name, patience, reward) in samples)
        {
            var path = $"Assets/Prefabs/Customers/{name}.prefab";
            // Check if it already exists
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                Debug.Log($"Skipping {name} — already exists");
                continue;
            }

            var spritePath = AssetDatabase.GetAssetPath(customerSpritePrefab);
            var root = PrefabUtility.LoadPrefabContents(spritePath);

            // Set customer data
            var customerFront = root.GetComponent<CustomerSeat>();
            if (customerFront != null)
            {
                var serialized = new SerializedObject(customerFront);
                var customerProp = serialized.FindProperty("customer");
                if (customerProp != null)
                {
                    var patienceProp = customerProp.FindPropertyRelative("Patience");
                    var rewardProp = customerProp.FindPropertyRelative("Reward");
                    if (patienceProp != null) patienceProp.intValue = patience;
                    if (rewardProp != null) rewardProp.intValue = reward;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            // Keep the customerComponentUi reference from the base prefab
            // (it's already assigned by SetupCustomerSprite)

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log($"✅ Created {name} (patience: {patience}, reward: {reward})");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("✅ Sample customers created!");
    }
}

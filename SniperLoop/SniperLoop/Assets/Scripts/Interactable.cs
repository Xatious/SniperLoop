using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Marker component for any pickupable/interactable object.
// Requires an "InteractMenu" prefab as a child for the world-space action menu.
// Drag the InteractMenu prefab from Assets/Prefabs onto this object as a child,
// then position it where you want the menu to appear.
[RequireComponent(typeof(Rigidbody))]
public class Interactable : MonoBehaviour
{
    [SerializeField] private string itemName = "Item";
    [SerializeField] private float holdDistance = 1.5f;

    [Header("Physics Overrides (0 = use system defaults)")]
    [SerializeField] private float springForce = 0f;
    [SerializeField] private float damping = 0f;

    [Header("Menu Appearance")]
    [SerializeField] private Color selectedColor = Color.yellow;
    [SerializeField] private Color unselectedColor = new Color(1f, 1f, 1f, 0.5f);

    public string ItemName => itemName;
    public float HoldDistance => holdDistance;
    public float SpringForce => springForce;
    public float Damping => damping;

    private Canvas worldCanvas;
    private TMP_Text titleText;
    private Transform menuRoot;
    private List<TMP_Text> menuItems = new List<TMP_Text>();
    private int selectedIndex;
    private bool menuVisible;
    private Camera mainCam;

    void Awake()
    {
        mainCam = Camera.main;

        // Find the InteractMenu child that was placed in the editor
        var menuTransform = transform.Find("InteractMenu(Clone)")
                         ?? transform.Find("InteractMenu");

        if (menuTransform != null)
        {
            worldCanvas = menuTransform.GetComponent<Canvas>();
            var panel = menuTransform.Find("Panel");
            if (panel != null)
            {
                titleText = panel.Find("Title")?.GetComponent<TMP_Text>();
                menuRoot = panel.Find("Actions");
            }
        }
        else
        {
            Debug.LogWarning($"Interactable '{itemName}' has no InteractMenu child. " +
                "Drag the InteractMenu prefab from Assets/Prefabs as a child of this object.");
        }

        HideMenu();
    }

    void LateUpdate()
    {
        // Billboard — always face the camera
        if (worldCanvas != null && mainCam != null && menuVisible)
        {
            worldCanvas.transform.LookAt(
                worldCanvas.transform.position + mainCam.transform.forward
            );
        }
    }

    public void ShowMenu(List<InteractionAction> actions)
    {
        if (worldCanvas == null) return;

        ClearMenuItems();

        if (titleText != null)
            titleText.text = itemName;

        foreach (var action in actions)
        {
            var itemGO = new GameObject("Action");
            itemGO.transform.SetParent(menuRoot, false);
            var txt = itemGO.AddComponent<TextMeshProUGUI>();
            txt.text = action.Label;
            txt.fontSize = 22;
            txt.alignment = TextAlignmentOptions.Center;
            txt.raycastTarget = false;

            var itemRect = itemGO.GetComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(0, 28);

            menuItems.Add(txt);
        }

        selectedIndex = 0;
        UpdateSelection();
        worldCanvas.gameObject.SetActive(true);
        menuVisible = true;
    }

    public void HideMenu()
    {
        if (worldCanvas != null)
            worldCanvas.gameObject.SetActive(false);
        menuVisible = false;
        ClearMenuItems();
    }

    public void ScrollMenu(int direction)
    {
        if (!menuVisible || menuItems.Count == 0) return;
        selectedIndex += direction;
        if (selectedIndex < 0) selectedIndex = menuItems.Count - 1;
        if (selectedIndex >= menuItems.Count) selectedIndex = 0;
        UpdateSelection();
    }

    public int GetSelectedIndex()
    {
        return menuVisible ? selectedIndex : -1;
    }

    void UpdateSelection()
    {
        for (int i = 0; i < menuItems.Count; i++)
        {
            var txt = menuItems[i];
            string label = txt.text.TrimStart('>', ' ');
            if (i == selectedIndex)
            {
                txt.color = selectedColor;
                txt.text = "> " + label;
            }
            else
            {
                txt.color = unselectedColor;
                txt.text = label;
            }
        }
    }

    void ClearMenuItems()
    {
        foreach (var item in menuItems)
            if (item != null) Destroy(item.gameObject);
        menuItems.Clear();
        selectedIndex = 0;
    }

    public List<InteractionAction> GetActions(InteractionSystem system)
    {
        var actions = new List<InteractionAction>();

        actions.Add(new InteractionAction("Pick Up", hand => system.PickupWithHand(hand, this)));

        var mountOut = GetComponentInChildren<MountPointOut>();
        if (mountOut != null && mountOut.IsSnapped)
        {
            actions.Insert(0, new InteractionAction("Enter Shooting Mode", hand =>
            {
                Debug.Log("Shooting mode not yet implemented");
            }));

            actions.Add(new InteractionAction("Remove from Mount", hand =>
            {
                mountOut.Unsnap();
                system.PickupWithHand(hand, this);
            }));
        }

        return actions;
    }
}

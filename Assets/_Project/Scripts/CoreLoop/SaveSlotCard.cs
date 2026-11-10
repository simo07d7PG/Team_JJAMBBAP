using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 저장 슬롯 칸 하나. ESC 메뉴(SaveMenu) 또는 로비(LobbySaveSelect)가 채운다. 버튼 OnClick → OnClick
public class SaveSlotCard : MonoBehaviour
{
    // 둘 중 하나만 채워진다
    [HideInInspector] public SaveMenu menu;
    [HideInInspector] public LobbySaveSelect lobby;
    [HideInInspector] public int index;

    public Image background;
    [Tooltip("칸 번호")]
    public TMP_Text slotText;
    [Tooltip("저장 정보 (빈 칸이면 '비어 있음')")]
    public TMP_Text infoText;
    public Button button;
    public TMP_Text buttonLabel;

    public void OnClick()
    {
        if (menu != null) menu.ClickSlot(index);
        else lobby.ClickSlot(index);
    }
}

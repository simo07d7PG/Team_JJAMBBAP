using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 저장 슬롯 칸 하나. SaveMenu가 채운다. 버튼 OnClick → OnClick
public class SaveSlotCard : MonoBehaviour
{
    [HideInInspector] public SaveMenu menu;
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
        menu.ClickSlot(index);
    }
}

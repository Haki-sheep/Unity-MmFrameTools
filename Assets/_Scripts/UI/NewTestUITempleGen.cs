/// <summary>
/// NewTestUITemple View层 - 自动生成，请勿手动修改
/// </summary>

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public partial class NewTestUITempleGen : MonoBehaviour
{
    public Image M_Image { get; private set; }
    public Image M_ImageA { get; private set; }
    public Image M_ImageB { get; private set; }

    private void Awake()
    {
        M_Image = GetComponent<Image>();
        M_ImageA = GetComponent<Image>();
        M_ImageB = GetComponent<Image>();
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WorldSpeedControl : MonoBehaviour
{
    [Header("UI 组件引用")]
    public Slider speedSlider;          // 拖入层级中的 Slider
    public TextMeshProUGUI speedText;   // 拖入层级中的 Text (TMP)

    [Header("设置")]
    public string textFormat = "CurrentSpeed: {0:F2}x"; // 显示格式，F2保留两位小数

    void Start()
    {
        // 脚本开始时，先根据 Slider 的当前滑块值初始化一次速度和文本
        if (speedSlider != null)
        {
            UpdateWorldSpeed(speedSlider.value);

            // 动态绑定监听事件：当滑块拖动时自动执行 UpdateWorldSpeed
            speedSlider.onValueChanged.AddListener(UpdateWorldSpeed);
        }
    }

    // 核心功能：更新世界时间缩放和文字显示
    public void UpdateWorldSpeed(float value)
    {
        // 设置 Unity 世界时间缩放 (0为暂停，1为正常，2为两倍速)
        Time.timeScale = value;

        // 更新文字显示
        if (speedText != null)
        {
            speedText.text = string.Format(textFormat, value);
        }
    }

    private void OnDestroy()
    {
        // 养成好习惯：销毁时移除监听
        if (speedSlider != null)
        {
            speedSlider.onValueChanged.RemoveListener(UpdateWorldSpeed);
        }
    }
}

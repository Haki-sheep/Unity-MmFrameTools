using MieMieFrameWork;
using MieMieFrameWork.UI;
using UnityEngine;

public class UITest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var uiMgr = ModuleHub.Instance.GetManager<UICoreMgr>();
        uiMgr.Init();
        uiMgr.ShowWindowAsync<NewTestUITemple>((uiObject)=>{
            if(uiObject!=null){
                Debug.Log("测试完毕");
            }
        });

         uiMgr.ShowWindowAsync<NewTestUI113>((uiObject)=>{
            if(uiObject!=null){
                Debug.Log("测试113完毕");
            }
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }


}

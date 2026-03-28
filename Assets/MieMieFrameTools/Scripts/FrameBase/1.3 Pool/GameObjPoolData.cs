namespace MieMieFrameWork.Pool
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    public class GameObjPoolData
    {
        // 类对象池父节点
        private Transform typeFather;
        private Queue<GameObject> poolQueue = new();
        /// <summary>
        /// 构造函数:设置对象结构并将对象入队
        /// </summary>
        /// <param name="obj">要放入的对象</param>
        /// <param name="poolRootObj">对象池根对象</param>
        public GameObjPoolData(GameObject obj, Transform AllGameObjectRoot, bool useFater = true)
        {
            if (useFater)
            {
                //如果二级节点为空 
                if (typeFather == null)
                    this.typeFather = new GameObject(obj.name + "Pool").transform;
                //如果总父节点为空
                if (typeFather.parent == null)
                    typeFather.transform.SetParent(AllGameObjectRoot);
            }
            PushGameObj(obj);
        }

        public void PushGameObj(GameObject obj)
        {
            if (this.typeFather != null)
                obj.transform.SetParent(typeFather);

            poolQueue.Enqueue(obj);
            obj.SetActive(false);
        }

        public GameObject GetGameObj(Transform parent = null)
        {
            if (poolQueue.Count > 0)
            {
                GameObject obj = poolQueue.Dequeue();
                obj.SetActive(true);
                obj.transform.SetParent(parent);
                // 移动到当前场景
                if (parent == null)
                    SceneManager.MoveGameObjectToScene(obj, SceneManager.GetActiveScene());

                return obj;
            }

            return null;
        }
    }
}
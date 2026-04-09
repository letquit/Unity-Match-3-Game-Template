using UnityEngine;

namespace Match3
{
    /// <summary>
    /// 宝石类。
    /// 代表棋盘上的一个独立方块，负责管理自身的类型数据和视觉表现。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))] // 确保组件存在，防止空引用
    public class Gem : MonoBehaviour
    {
        // 当前宝石的类型（例如：红色宝石、蓝色宝石等）
        // 通常这是一个 ScriptableObject 或包含图片/分值数据的类
        public GemType type;

        /// <summary>
        /// 设置宝石的类型并更新外观。
        /// 这对于对象池复用非常重要：可以将一个旧的宝石直接“变身”为新类型。
        /// </summary>
        public void SetType(GemType type)
        {
            this.type = type;
            // 获取 SpriteRenderer 组件并将图片更新为该类型对应的图片
            GetComponent<SpriteRenderer>().sprite = type.sprite;
        }

        // 获取当前宝石的类型数据
        public GemType GetType() => type;

        /// <summary>
        /// 销毁宝石。
        /// 从场景中移除该游戏对象。
        /// （注：在优化版中，这里通常会改为回收到对象池，而不是直接 Destroy）
        /// </summary>
        public void DestroyGem() => Destroy(gameObject);
    }
}
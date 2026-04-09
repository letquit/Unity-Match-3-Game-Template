using UnityEngine;

namespace Match3
{
    /// <summary>
    /// 宝石类型定义（ScriptableObject）。
    /// 用于在 Unity 编辑器中创建可复用的宝石数据资源（如红宝石、蓝宝石等）。
    /// 它存储了宝石的静态属性，实现了数据与逻辑的分离。
    /// </summary>
    // 该特性允许在编辑器菜单中右键创建此类型的资源：Create -> Match3 -> GemType
    [CreateAssetMenu(fileName = "GemType", menuName = "Match3/GemType")]
    public class GemType : ScriptableObject
    {
        /// <summary>
        /// 宝石的视觉图片。
        /// 在 Inspector 面板中将具体的 Sprite 图片拖入此处，以定义这种宝石的外观。
        /// </summary>
        public Sprite sprite;
    }
}
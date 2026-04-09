using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Match3
{
    /// <summary>
    /// 音频管理器。
    /// 负责管理游戏中的所有音效播放。
    /// 通过封装播放逻辑，为外部提供简单的调用接口，并处理音调随机化以增加听觉丰富度。
    /// </summary>
    [RequireComponent(typeof(AudioSource))] // 确保挂载 AudioSource 组件
    public class AudioManager : MonoBehaviour
    {
        // 点击宝石时的音效
        [SerializeField] private AudioClip click;
        // 取消选择时的音效
        [SerializeField] private AudioClip deselect;
        // 匹配成功时的音效
        [SerializeField] private AudioClip match;
        // 匹配失败时的音效
        [SerializeField] private AudioClip noMatch;
        // 宝石移动/交换时的呼啸音效
        [SerializeField] private AudioClip whoosh;
        // 宝石消除/爆破时的音效
        [SerializeField] private AudioClip pop;
        
        // 音频源组件引用
        private AudioSource audioSource;

        /// <summary>
        /// 编辑器验证回调。
        /// 当脚本在 Inspector 中被修改或重置时，自动获取 AudioSource 组件引用。
        /// </summary>
        private void OnValidate()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
        }

        // 播放点击音效
        public void PlayClick() => audioSource.PlayOneShot(click);
        
        // 播放取消选择音效
        public void PlayDeselect() => audioSource.PlayOneShot(deselect);
        
        // 播放匹配成功音效
        public void PlayMatch() => audioSource.PlayOneShot(match);
        
        // 播放匹配失败音效
        public void PlayNoMatch() => audioSource.PlayOneShot(noMatch);
        
        // 播放移动音效（带随机音调）
        public void PlayWhoosh() => PlayRandomPitch(whoosh);
        
        // 播放消除音效（带随机音调）
        public void PlayPop() => PlayRandomPitch(pop);

        /// <summary>
        /// 播放带随机音调的音效。
        /// 通过微调音调（Pitch），避免重复播放同一音效时的单调感。
        /// </summary>
        /// <param name="audioClip">要播放的音频片段</param>
        private void PlayRandomPitch(AudioClip audioClip)
        {
            // 在 0.8 到 1.2 之间随机设置音调
            audioSource.pitch = Random.Range(0.8f, 1.2f);
            audioSource.PlayOneShot(audioClip);
            // 播放完成后恢复默认音调，避免影响其他音效
            audioSource.pitch = 1f;
        }
    }
}
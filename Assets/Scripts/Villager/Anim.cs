using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AnimType
{
    Idle,
    Walk,
    Carry,
    Chop ,
    Plow,
    Shoot,
    Pick,
    Talk,
    Fish,
    FishPull,
    LookPoint,
    FarmSeed,
    FarmWater,
    FarmPick
}
public class Anim : MonoBehaviour
{
    Animator anim;
    AnimType current = AnimType.Idle;
    // ブレンドにかける時間（秒）
    [SerializeField] float blendDuration = 0.8f;

    void Awake()
    {
        anim = GetComponent<Animator>();
    }
    public void Play(AnimType next)
    {
        if (next == current) return;

        // ステート名は Animator Controller のステート名と一致させておく
        string stateName = next.ToString();

        // ハッシュを取っておくと早いです（任意）
        int stateHash = Animator.StringToHash(stateName);

        // 指定のステートにクロスフェード
        anim.CrossFade(stateHash, blendDuration);

        current = next;
        //Debug.Log($"[Anim] CrossFading to {next} over {blendDuration}s");
    }
}

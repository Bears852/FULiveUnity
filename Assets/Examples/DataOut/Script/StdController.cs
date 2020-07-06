﻿using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using NatCamU.Core;
using NatCamU.Dispatch;

public class StdController : MonoBehaviour
{
    //这个组件用于人脸的跟踪，包括位移，旋转和表情系数（BlengShape），放在人脸的mesh上
    //！！！仅供参考，没有考虑效率问题和易用性问题！！！

    public RenderToModel rtm;

    Quaternion m_rotation0; //人脸初始旋转
    Vector3 m_position0;    //人脸初始位置

    /////////////////////////////////////
    //unity blendshape
    public SkinnedMeshRenderer[] skinnedMeshRenderers;  //人脸的Render，用来设置表情系数
    bool pauseUpdate = false;   //暂停更新

    public int faceid = 0;  //人脸ID

    //左右调换部分BlendShape数据,使其镜像
    private int[] mirrorBlendShape = new int[56] {1,0, 3,2, 5,4, 7,6, 9,8,
                                                   11,10, 13,12, 15,14, 16,
                                                   18,17, 19,
                                                   22,21,20,
                                                   24,23, 26,25, 28,27, 30,29, 32,31,
                                                   33,34,35,36,37,38,39,40,41,42,43, 45,44,
                                                   46,49,48,47,52,51,50,55,54,53,
                                                 };


    //初始化时记录原始信息
    void Awake()
    {
        m_rotation0 = transform.localRotation;
        m_position0 = transform.localPosition;
    }

    //相机切换完成回调
    void Start()
    {
        //skinnedMeshRenderer.enabled = false;
        rtm.onSwitchCamera += OnSwitchCamera;
    }

    //切换相机时暂停更新，防止乱跑
    void OnSwitchCamera(bool isSwitching)
    {
        pauseUpdate = isSwitching;
    }

    //每帧更新人脸信息
    void Update()
    {
        if (pauseUpdate)
            return;
        if (FaceunityWorker.instance == null || FaceunityWorker.instance.m_plugin_inited == false) { return; }
        if (faceid >= FaceunityWorker.instance.m_need_update_facenum)
        {
            return;
        }
        if (FaceunityWorker.instance.m_need_update_facenum > 0)    //仅在跟踪到人脸的情况下更新
        {
            //skinnedMeshRenderer.enabled = true;
        }
        else
        {
            //skinnedMeshRenderer.enabled = false;
            return;
        }

        float[] R = FaceunityWorker.instance.m_rotation[faceid].m_data; //人脸旋转数据
        float[] P = FaceunityWorker.instance.m_translation[faceid].m_data;  //人脸位移数据
        float[] E = FaceunityWorker.instance.m_expression_with_tongue[faceid].m_data; //人脸表情数据

        bool ifMirrored = NatCam.Camera.Facing == Facing.Front; //是否镜像
#if (UNITY_ANDROID) && (!UNITY_EDITOR)
        ifMirrored = !ifMirrored;
#elif (UNITY_IOS) && (!UNITY_EDITOR)
        ifMirrored=false;
#endif

        if (ifMirrored)
        {
            for (int j = 0; j < skinnedMeshRenderers.Length; j++)
            {
                for (int i = 0; i < skinnedMeshRenderers[j].sharedMesh.blendShapeCount; i++)
                {
                    skinnedMeshRenderers[j].SetBlendShapeWeight(mirrorBlendShape[i], E[i] * 100);    //SDK输出表情系数数据为0~1，一般Unity的BlendShape系数为0~100，因此需要调整
                }
            }
            transform.localRotation = m_rotation0 * PostProcessRotation(new Quaternion(-R[0], -R[1], R[2], R[3]));   //沿着yz平面镜像
            if (rtm.ifTrackPos == true)
                transform.localPosition = PostProcessPositon(new Vector3(-P[0], P[1], P[2]));
            else
                transform.localPosition = m_position0;
        }
        else
        {
            for (int j = 0; j < skinnedMeshRenderers.Length; j++)
            {
                for (int i = 0; i < skinnedMeshRenderers[j].sharedMesh.blendShapeCount; i++)
                {
                    skinnedMeshRenderers[j].SetBlendShapeWeight(i, E[i] * 100);
                }
            }
            transform.localRotation = m_rotation0 * PostProcessRotation(new Quaternion(-R[0], R[1], -R[2], R[3]));   //坐标系转换
            if (rtm.ifTrackPos == true)
                transform.localPosition = PostProcessPositon(new Vector3(P[0], P[1], P[2]));
            else
                transform.localPosition = m_position0;
        }
        //Debug.Log("STDUpdate:localRotation="+ transform.localEulerAngles.x+","+ transform.localEulerAngles.y + "," + transform.localEulerAngles.z);
    }

    Quaternion PostProcessRotation(Quaternion r)
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        return Quaternion.AngleAxis(180, Vector3.forward) * r;
#elif !UNITY_EDITOR && UNITY_ANDROID
        return Quaternion.AngleAxis(90, Vector3.forward) * r;
#elif !UNITY_EDITOR && UNITY_IOS
        return Quaternion.AngleAxis(180, Vector3.forward) * r;
#else
        return r;
#endif
    }
    Vector3 PostProcessPositon(Vector3 p)
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        return new Vector3(-p.x, -p.y, p.z);
#elif !UNITY_EDITOR && UNITY_ANDROID
        return new Vector3(p.y, -p.x, p.z);
#elif !UNITY_EDITOR && UNITY_IOS
        return new Vector3(-p.x, -p.y, p.z);
#else
        return p;
#endif
    }

    //重置人脸的位置旋转
    public void ResetTransform()
    {
        transform.localPosition = m_position0;
        transform.localRotation = m_rotation0;
        //Debug.Log("ResetTransform:localRotation=" + transform.localEulerAngles.x + "," + transform.localEulerAngles.y + "," + transform.localEulerAngles.z);
    }
}

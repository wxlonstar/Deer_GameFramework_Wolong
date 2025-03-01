﻿using Deer;
using GameFramework;
using GameFramework.Resource;
using GameFramework.UI;
using Main.Runtime;
using Main.Runtime.Procedure;
using System;
using System.Collections.Generic;
using System.Reflection;
using HotfixFramework.Runtime;
using UGFExtensions.SpriteCollection;
using UGFExtensions.Texture;
using UnityEngine;
using UnityGameFramework.Runtime;
/// <summary>
/// 游戏入口。
/// </summary>
public partial class GameEntry
{

    public static MessengerComponent Messenger => _messenger ??= UnityGameFramework.Runtime.GameEntry.GetComponent<MessengerComponent>();
    private static MessengerComponent _messenger;

    public static CameraComponent Camera => _camera ??= UnityGameFramework.Runtime.GameEntry.GetComponent<CameraComponent>();
    private static CameraComponent _camera;

    public static NetConnectorComponent NetConnector => _netConnector ??= UnityGameFramework.Runtime.GameEntry.GetComponent<NetConnectorComponent>();
    private static NetConnectorComponent _netConnector;

    public static ConfigComponent Config => _config ??= UnityGameFramework.Runtime.GameEntry.GetComponent<ConfigComponent>();
    private static ConfigComponent _config;

    public static MainThreadDispatcherComponent MainThreadDispatcher => _mainThreadDispatcher ??= UnityGameFramework.Runtime.GameEntry.GetComponent<MainThreadDispatcherComponent>();
    private static MainThreadDispatcherComponent _mainThreadDispatcher;

    public static TextureSetComponent TextureSet => _textureSet ??= UnityGameFramework.Runtime.GameEntry.GetComponent<TextureSetComponent>();
    private static TextureSetComponent _textureSet;

    public static SpriteCollectionComponent SpriteCollection => _spriteCollection ??= UnityGameFramework.Runtime.GameEntry.GetComponent<SpriteCollectionComponent>();
    private static SpriteCollectionComponent _spriteCollection;

    public static TimerComponent Timer => _timer ??= UnityGameFramework.Runtime.GameEntry.GetComponent<TimerComponent>();
    private static TimerComponent _timer;

    public static AssetObjectComponent AssetObject => _assetObject ??= UnityGameFramework.Runtime.GameEntry.GetComponent<AssetObjectComponent>();
    private static AssetObjectComponent _assetObject;


    private static void InitCustomDebuggers()
    {
        // 将来在这里注册自定义的调试器
        GMNetWindow netWindow = new GMNetWindow();
        Debugger.SetGMNetWindowHelper(netWindow);

        CustomSettingsWindow customSettingWindow = new CustomSettingsWindow();
        Debugger.SetCustomSettingWindowHelper(customSettingWindow);
    }
    /// <summary>
    /// 初始化组件一些设置
    /// </summary>
    private static void InitComponentsSet()
    {

    }
    /// <summary>
    /// 加载自定义组件
    /// </summary>
    private static void LoadCustomComponent() 
    {
        GameEntryMain.Resource.LoadAsset("Assets/Deer/AssetsHotfix/GF/Customs.prefab", new LoadAssetCallbacks(loadAssetSuccessCallback,loadAssetFailureCallback));
    }

    private static void loadAssetFailureCallback(string assetName, LoadResourceStatus status, string errorMessage, object userData)
    {
        
    }

    private static void loadAssetSuccessCallback(string assetName, object asset, float duration, object userData)
    {
        if (GameObject.Find("DeerGF/Customs")!= null)
        {
            Resource.UnloadAsset(asset);
            return;
        }
        GameObject gameObject = UnityEngine.Object.Instantiate((GameObject)asset, GameObject.Find("DeerGF").transform, true);
        gameObject.name = "Customs";
        gameObject.transform.position = Vector3.zero;
        ResetProcedure();
        ResetUIFormHelper();
        //关闭启动界面
        GameEntryMain.UI.DeerUIInitRootForm().OnCloseLaunchView();
        GameEntryMain.UI.DeerUIInitRootForm().OnOpenLoadingForm(false);
    }
    private static List<Assembly> m_HotfixAssemblys;
    private static ProcedureBase m_EntranceProcedureBase;
    private static string m_EntranceProcedureTypeName = "HotfixBusiness.Procedure.ProcedurePreload";
    private static void ResetProcedure() 
    {
#if UNITY_EDITOR
        if (m_HotfixAssemblys.Count == 0)
        {
            Logger.Error("1.请检查GlobalSettings.asset 文件里的 HotfixAssemblies 集合字段，确保热更程序集已经收集完毕；");
            return;
        }
#endif
        //卸载流程
        Fsm.DestroyFsm<GameFramework.Procedure.IProcedureManager>();
        GameFramework.Procedure.IProcedureManager procedureManager = GameFramework.GameFrameworkEntry.GetModule<GameFramework.Procedure.IProcedureManager>();
        //创建新的流程 HotfixFramework.Runtime
        string[] m_ProcedureTypeNames = TypeUtils.GetRuntimeTypeNames(typeof(ProcedureBase), m_HotfixAssemblys);
        ProcedureBase[] procedures = new ProcedureBase[m_ProcedureTypeNames.Length];
        for (int i = 0; i < m_ProcedureTypeNames.Length; i++)
        {
            Type procedureType = GameFramework.Utility.Assembly.GetType(m_ProcedureTypeNames[i]);
            if (procedureType == null)
            {
                Log.Error("Can not find procedure type '{0}'.", m_ProcedureTypeNames[i]);
                return;
            }

            procedures[i] = (ProcedureBase)Activator.CreateInstance(procedureType);
            if (procedures[i] == null)
            {
                Log.Error("Can not create procedure instance '{0}'.", m_ProcedureTypeNames[i]);
                return;
            }

            if (m_EntranceProcedureTypeName == m_ProcedureTypeNames[i])
            {
                m_EntranceProcedureBase = procedures[i];
            }
        }

        if (m_EntranceProcedureBase == null)
        {
            Log.Error("Entrance procedure is invalid.");
            return;
        }
        procedureManager.Initialize(GameFramework.GameFrameworkEntry.GetModule<GameFramework.Fsm.IFsmManager>(), procedures);
        procedureManager.StartProcedure(m_EntranceProcedureBase.GetType());
    }
    private static string m_UIFormHelperTypeName = "Main.Runtime.DeerUIFormHelper";
    private static UIFormHelperBase m_CustomUIFormHelper = null;
    private static void ResetUIFormHelper() 
    {
        IUIManager uIManager = GameFrameworkEntry.GetModule<IUIManager>();
        if (uIManager == null)
        {
            Log.Fatal("UI manager is invalid.");
            return;
        }
        GameObject uIFormHelper = GameObject.Find("UI Form Helper");
        UIFormHelperBase uiFormHelper = Helper.CreateHelper(m_UIFormHelperTypeName, m_CustomUIFormHelper);
        if (uiFormHelper == null)
        {
            Log.Error("Can not create UI form helper.");
            return;
        }

        uiFormHelper.name = "UI Form Helper Custom";
        Transform transform = uiFormHelper.transform;
        transform.SetParent(uIFormHelper.transform.parent);
        transform.SetAsFirstSibling();
        transform.localScale = Vector3.one;
        uIManager.SetUIFormHelper(uiFormHelper);

        foreach (var item in Constant.UI.UIGroups)
        {
            UI.AddUIGroup(item.Key, item.Value, false);
        }
    }
    public static void Entrance(object[] objects) 
    {
        m_HotfixAssemblys = (List<Assembly>)objects[0];
        //初始化自定义调试器
        InitCustomDebuggers();
        InitComponentsSet();
        LoadCustomComponent();
    }
}

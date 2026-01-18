using LWCore;
using LWUI;
using UnityEngine;
using UnityEngine.UI;

[UIViewData("Assets/0Res/Prefabs/UI/LoginView.prefab", (int)FindType.Name, "LWFramework/Canvas/Normal")]
public class LoginView : BaseUIView
{
    private const string EVENT_LOGIN_SUBMIT = "Auth.Login.Submit";
    private const string EVENT_LOGIN_CANCEL = "Auth.Login.Cancel";
    private const string PLAYER_PREFS_REMEMBER = "Auth.Login.Remember";
    private const string PLAYER_PREFS_USER = "Auth.Login.User";
    private const string PLAYER_PREFS_PASSWORD = "Auth.Login.Password";


    [UIElement("PnlCard/IpfAccount")]
    private InputField m_IpfAccount;
    [UIElement("PnlCard/IpfPassword")]
    private InputField m_IpfPassword;
    [UIElement("PnlCard/PnlOptions/TglRemember")]
    private Toggle m_TglRemember;
    [UIElement("PnlCard/PnlOptions/BtnForgot")]
    private Button m_BtnForgot;
    [UIElement("PnlCard/BtnLogin")]
    private Button m_BtnLogin;
    [UIElement("PnlCard/PnlRegister/BtnRegister")]
    private Button m_BtnRegister;
    [UIElement("BtnClose")]
    private Button m_BtnClose;


    /// <summary>
    /// 创建并初始化登录界面（绑定控件与按钮事件）。
    /// </summary>
    /// <param name="gameObject">界面实体对象</param>
    public override void CreateView(GameObject gameObject)
    {
        base.CreateView(gameObject);
        BindEvents();
        LoadRememberedUser();
    }

    /// <summary>
    /// 清理界面（移除事件绑定并销毁实体）。
    /// </summary>
    public override void ClearView()
    {
        UnbindEvents();
        base.ClearView();
    }

    /// <summary>
    /// 绑定按钮点击事件。
    /// </summary>
    private void BindEvents()
    {
        if (m_BtnLogin != null)
        {
            m_BtnLogin.onClick.AddListener(OnClickLogin);
        }
        if (m_BtnClose != null)
        {
            m_BtnClose.onClick.AddListener(OnClickClose);
        }
    }

    /// <summary>
    /// 移除按钮点击事件，避免重复绑定与泄漏。
    /// </summary>
    private void UnbindEvents()
    {
        if (m_BtnLogin != null)
        {
            m_BtnLogin.onClick.RemoveListener(OnClickLogin);
        }
        if (m_BtnClose != null)
        {
            m_BtnClose.onClick.RemoveListener(OnClickClose);
        }
    }

    /// <summary>
    /// 点击登录：派发登录提交事件。
    /// </summary>
    private void OnClickLogin()
    {
        string userName = GetTrimmedText(m_IpfAccount);
        string password = GetText(m_IpfPassword);
        bool isRemember = m_TglRemember != null && m_TglRemember.isOn;

        SaveRememberedUser(isRemember, userName);
        ManagerUtility.EventMgr.DispatchEvent<string, string, bool>(EVENT_LOGIN_SUBMIT, userName, password, isRemember);
    }

    /// <summary>
    /// 点击关闭：派发取消登录事件。
    /// </summary>
    private void OnClickClose()
    {
        ManagerUtility.EventMgr.DispatchEvent(EVENT_LOGIN_CANCEL);
    }

    /// <summary>
    /// 从本地读取“记住账号”信息并回填。
    /// </summary>
    private void LoadRememberedUser()
    {
        int rememberValue = PlayerPrefs.GetInt(PLAYER_PREFS_REMEMBER, 0);
        bool isRemember = rememberValue == 1;
        string userName = PlayerPrefs.GetString(PLAYER_PREFS_USER, string.Empty);
        string password = PlayerPrefs.GetString(PLAYER_PREFS_PASSWORD, string.Empty);

        if (m_TglRemember != null)
        {
            m_TglRemember.isOn = isRemember;
        }
        if (isRemember && m_IpfAccount != null)
        {
            m_IpfAccount.text = userName;
        }
        if (isRemember && m_IpfPassword != null)
        {
            m_IpfPassword.text = password;
        }
    }

    /// <summary>
    /// 持久化“记住账号”开关与账号（不保存密码）。
    /// </summary>
    /// <param name="isRemember">是否记住账号</param>
    /// <param name="userName">账号</param>
    private void SaveRememberedUser(bool isRemember, string userName)
    {
        PlayerPrefs.SetInt(PLAYER_PREFS_REMEMBER, isRemember ? 1 : 0);
        if (isRemember)
        {
            PlayerPrefs.SetString(PLAYER_PREFS_USER, userName);
            PlayerPrefs.SetString(PLAYER_PREFS_PASSWORD, m_IpfPassword.text);
        }
        else
        {
            PlayerPrefs.DeleteKey(PLAYER_PREFS_PASSWORD);
            PlayerPrefs.DeleteKey(PLAYER_PREFS_USER);
        }

        PlayerPrefs.Save();
    }

    /// <summary>
    /// 获取输入框文本（Trim 后）。
    /// </summary>
    /// <param name="inputField">输入框</param>
    /// <returns>去空白后的文本</returns>
    private string GetTrimmedText(InputField inputField)
    {
        string text = GetText(inputField);
        return text.Trim();
    }

    /// <summary>
    /// 安全获取输入框文本。
    /// </summary>
    /// <param name="inputField">输入框</param>
    /// <returns>文本（为空则返回空字符串）</returns>
    private string GetText(InputField inputField)
    {
        if (inputField == null)
        {
            return string.Empty;
        }
        return inputField.text;
    }
}

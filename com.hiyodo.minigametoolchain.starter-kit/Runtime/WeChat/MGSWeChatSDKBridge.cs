using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MiniGame.StarterKit.Runtime.WeChat
{
    /// <summary>
    /// 微信小游戏 SDK 反射桥接层。
    /// 通过运行时反射调用 com.qq.weixin.minigame 中的 API，避免 Starter Kit 在编译期硬依赖 WeChatWASM。
    /// 当用户未安装微信 SDK 时，桥接层不可用，各适配器会安全回退到 Editor Mock / 失败回调。
    /// </summary>
    internal static class MGSWeChatSDKBridge
    {
        public static bool IsAvailable { get; private set; }

        private static Type _wxType;
        private static MethodInfo _createRewardedVideoAd;
        private static MethodInfo _createBannerAd;
        private static MethodInfo _getWindowInfo;
        private static MethodInfo _checkIsSupportMidasPayment;
        private static MethodInfo _requestMidasPayment;

        private static Type _rewardedVideoAdType;
        private static MethodInfo _rvOnError;
        private static Type _rvOnErrorArgType;
        private static MethodInfo _rvOnClose;
        private static Type _rvOnCloseArgType;
        private static MethodInfo _rvLoad;
        private static Type _rvLoadSuccessArgType;
        private static Type _rvLoadFailedArgType;
        private static MethodInfo _rvShow;
        private static Type _rvShowSuccessArgType;
        private static Type _rvShowFailedArgType;
        private static MethodInfo _rvDestroy;

        private static Type _bannerAdType;
        private static MethodInfo _bannerOnError;
        private static Type _bannerOnErrorArgType;
        private static MethodInfo _bannerOnLoad;
        private static Type _bannerOnLoadArgType;
        private static MethodInfo _bannerShow;
        private static Type _bannerShowSuccessArgType;
        private static Type _bannerShowFailedArgType;
        private static MethodInfo _bannerHide;
        private static MethodInfo _bannerDestroy;

        private static Type _rewardedParamType;
        private static Type _bannerParamType;
        private static Type _styleType;
        private static Type _windowInfoType;
        private static Type _safeAreaType;
        private static Type _checkOptionType;
        private static Type _requestOptionType;

        static MGSWeChatSDKBridge()
        {
            Initialize();
        }

        private static void Initialize()
        {
            try
            {
                _wxType = FindType("WeChatWASM.WX");
                if (_wxType == null) return;

                _createRewardedVideoAd = FindStaticMethod(_wxType, "CreateRewardedVideoAd");
                _createBannerAd = FindStaticMethod(_wxType, "CreateBannerAd");
                _getWindowInfo = FindStaticMethod(_wxType, "GetWindowInfo");
                _checkIsSupportMidasPayment = FindStaticMethod(_wxType, "CheckIsSupportMidasPayment");
                _requestMidasPayment = FindStaticMethod(_wxType, "RequestMidasPayment");

                _rewardedParamType = FindType("WeChatWASM.WXCreateRewardedVideoAdParam");
                _bannerParamType = FindType("WeChatWASM.WXCreateBannerAdParam");
                _styleType = FindType("WeChatWASM.Style");
                _windowInfoType = FindType("WeChatWASM.WindowInfo");
                _safeAreaType = FindType("WeChatWASM.SafeArea");
                _checkOptionType = FindType("WeChatWASM.CheckIsSupportMidasPaymentOption");
                _requestOptionType = FindType("WeChatWASM.RequestMidasPaymentOption");

                _rewardedVideoAdType = FindType("WeChatWASM.WXRewardedVideoAd");
                if (_rewardedVideoAdType != null)
                {
                    _rvOnError = FindActionMethod(_rewardedVideoAdType, "OnError", out _rvOnErrorArgType);
                    _rvOnClose = FindActionMethod(_rewardedVideoAdType, "OnClose", out _rvOnCloseArgType);

                    var load = FindActionPairMethod(_rewardedVideoAdType, "Load", out _rvLoadSuccessArgType, out _rvLoadFailedArgType);
                    if (load != null)
                    {
                        _rvLoad = load;
                    }

                    var show = FindActionPairMethod(_rewardedVideoAdType, "Show", out _rvShowSuccessArgType, out _rvShowFailedArgType);
                    if (show != null)
                    {
                        _rvShow = show;
                    }

                    _rvDestroy = _rewardedVideoAdType.GetMethod("Destroy", BindingFlags.Public | BindingFlags.Instance);
                }

                _bannerAdType = FindType("WeChatWASM.WXBannerAd");
                if (_bannerAdType != null)
                {
                    _bannerOnError = FindActionMethod(_bannerAdType, "OnError", out _bannerOnErrorArgType);
                    _bannerOnLoad = FindActionMethod(_bannerAdType, "OnLoad", out _bannerOnLoadArgType);

                    var show = FindActionPairMethod(_bannerAdType, "Show", out _bannerShowSuccessArgType, out _bannerShowFailedArgType);
                    if (show != null)
                    {
                        _bannerShow = show;
                    }

                    _bannerHide = _bannerAdType.GetMethod("Hide", BindingFlags.Public | BindingFlags.Instance);
                    _bannerDestroy = _bannerAdType.GetMethod("Destroy", BindingFlags.Public | BindingFlags.Instance);
                }

                IsAvailable = _createRewardedVideoAd != null
                    && _createBannerAd != null
                    && _getWindowInfo != null
                    && _rewardedVideoAdType != null
                    && _bannerAdType != null
                    && _requestOptionType != null
                    && _checkOptionType != null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MGSWeChatSDKBridge] 初始化反射桥接失败: {ex.Message}");
                IsAvailable = false;
            }
        }

        #region Safe Area

        public static bool TryGetSafeArea(out Rect safeArea)
        {
            safeArea = new Rect();
            if (!IsAvailable || _getWindowInfo == null || _windowInfoType == null || _safeAreaType == null)
                return false;

            try
            {
                var info = _getWindowInfo.Invoke(null, null);
                if (info == null) return false;
                var area = GetFieldValue(info, "safeArea");
                if (area == null) return false;

                safeArea = new Rect(
                    (float)GetFieldValue<double>(area, "left"),
                    (float)GetFieldValue<double>(area, "top"),
                    (float)GetFieldValue<double>(area, "width"),
                    (float)GetFieldValue<double>(area, "height"));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MGSWeChatSDKBridge] 读取微信安全区失败: {ex.Message}");
                return false;
            }
        }

        public static double GetWindowHeight()
        {
            if (!IsAvailable) return 0;
            try
            {
                var info = _getWindowInfo.Invoke(null, null);
                return GetFieldValue<double>(info, "windowHeight");
            }
            catch
            {
                return 0;
            }
        }

        #endregion

        #region Rewarded Video

        public static object CreateRewardedVideoAd(string adUnitId)
        {
            if (!IsAvailable) return null;
            var param = Activator.CreateInstance(_rewardedParamType);
            SetField(param, "adUnitId", adUnitId);
            return _createRewardedVideoAd.Invoke(null, new[] { param });
        }

        public static void RewardedVideoOnError(object ad, Action<object> callback)
        {
            if (ad == null || _rvOnError == null) return;
            var del = CreateActionDelegate(_rvOnErrorArgType, callback);
            _rvOnError.Invoke(ad, new[] { del });
        }

        public static void RewardedVideoOnClose(object ad, Action<object> callback)
        {
            if (ad == null || _rvOnClose == null) return;
            var del = CreateActionDelegate(_rvOnCloseArgType, callback);
            _rvOnClose.Invoke(ad, new[] { del });
        }

        public static void RewardedVideoLoad(object ad, Action<object> onSuccess, Action<object> onFailed)
        {
            if (ad == null || _rvLoad == null) return;
            var successDel = CreateActionDelegate(_rvLoadSuccessArgType, onSuccess);
            var failedDel = CreateActionDelegate(_rvLoadFailedArgType, onFailed);
            _rvLoad.Invoke(ad, new[] { successDel, failedDel });
        }

        public static void RewardedVideoShow(object ad, Action<object> onSuccess, Action<object> onFailed)
        {
            if (ad == null || _rvShow == null) return;
            var successDel = CreateActionDelegate(_rvShowSuccessArgType, onSuccess);
            var failedDel = CreateActionDelegate(_rvShowFailedArgType, onFailed);
            _rvShow.Invoke(ad, new[] { successDel, failedDel });
        }

        public static void RewardedVideoDestroy(object ad)
        {
            if (ad == null || _rvDestroy == null) return;
            _rvDestroy.Invoke(ad, null);
        }

        #endregion

        #region Banner

        public static object CreateBannerAd(string adUnitId, int width, int height, int top, int adIntervals)
        {
            if (!IsAvailable) return null;
            var param = Activator.CreateInstance(_bannerParamType);
            SetField(param, "adUnitId", adUnitId);
            SetField(param, "adIntervals", adIntervals);

            var style = Activator.CreateInstance(_styleType);
            SetField(style, "left", 0);
            SetField(style, "top", top);
            SetField(style, "width", width);
            SetField(style, "height", height);
            SetField(param, "style", style);

            return _createBannerAd.Invoke(null, new[] { param });
        }

        public static void BannerOnError(object ad, Action<object> callback)
        {
            if (ad == null || _bannerOnError == null) return;
            var del = CreateActionDelegate(_bannerOnErrorArgType, callback);
            _bannerOnError.Invoke(ad, new[] { del });
        }

        public static void BannerOnLoad(object ad, Action<object> callback)
        {
            if (ad == null || _bannerOnLoad == null) return;
            var del = CreateActionDelegate(_bannerOnLoadArgType, callback);
            _bannerOnLoad.Invoke(ad, new[] { del });
        }

        public static void BannerShow(object ad, Action<object> onSuccess, Action<object> onFailed)
        {
            if (ad == null || _bannerShow == null) return;
            var successDel = CreateActionDelegate(_bannerShowSuccessArgType, onSuccess);
            var failedDel = CreateActionDelegate(_bannerShowFailedArgType, onFailed);
            _bannerShow.Invoke(ad, new[] { successDel, failedDel });
        }

        public static void BannerHide(object ad)
        {
            if (ad == null || _bannerHide == null) return;
            _bannerHide.Invoke(ad, null);
        }

        public static void BannerDestroy(object ad)
        {
            if (ad == null || _bannerDestroy == null) return;
            _bannerDestroy.Invoke(ad, null);
        }

        #endregion

        #region IAP

        public static void CheckIsSupportMidasPayment(Action<object> onSuccess, Action<object> onFailed)
        {
            if (!IsAvailable) return;
            var option = Activator.CreateInstance(_checkOptionType);
            SetCallback(option, "success", onSuccess);
            SetCallback(option, "fail", onFailed);
            _checkIsSupportMidasPayment.Invoke(null, new[] { option });
        }

        public static void RequestMidasPayment(MGSWeChatPaymentInfo info, Action<object> onSuccess, Action<object> onFailed)
        {
            if (!IsAvailable || info == null) return;
            var option = Activator.CreateInstance(_requestOptionType);
            SetField(option, "mode", info.Mode);
            SetField(option, "offerId", info.OfferId);
            SetField(option, "currencyType", info.CurrencyType);
            SetField(option, "buyQuantity", (int?)info.BuyQuantity);
            SetField(option, "outTradeNo", info.OutTradeNo);
            SetField(option, "env", (int?)info.Env);
            SetField(option, "platform", info.Platform);
            SetField(option, "zoneId", info.ZoneId);
            SetCallback(option, "success", onSuccess);
            SetCallback(option, "fail", onFailed);
            _requestMidasPayment.Invoke(null, new[] { option });
        }

        #endregion

        #region Error Message

        public static string GetErrorMessage(object response)
        {
            if (response == null) return null;
            try
            {
                var msg = GetFieldValue<string>(response, "errMsg");
                return msg;
            }
            catch
            {
                return null;
            }
        }

        public static bool GetIsEnded(object response)
        {
            if (response == null) return false;
            try
            {
                if (GetFieldValue<bool>(response, "isEnded", out var val)) return val;
            }
            catch { }
            return false;
        }

        #endregion

        #region Reflection Helpers

        private static Type FindType(string fullName)
        {
            var t = Type.GetType(fullName);
            if (t != null) return t;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                t = asm.GetType(fullName);
                if (t != null) return t;
            }
            return null;
        }

        private static MethodInfo FindStaticMethod(Type type, string name)
        {
            return type.GetMethod(name, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        }

        private static MethodInfo FindActionMethod(Type type, string name, out Type argType)
        {
            argType = null;
            var method = type.GetMethod(name, BindingFlags.Public | BindingFlags.Instance);
            if (method == null) return null;
            var parameters = method.GetParameters();
            if (parameters.Length != 1) return null;
            var paramType = parameters[0].ParameterType;
            if (!paramType.IsGenericType || paramType.GetGenericTypeDefinition() != typeof(Action<>))
                return null;
            argType = paramType.GetGenericArguments()[0];
            return method;
        }

        private static MethodInfo FindActionPairMethod(Type type, string name, out Type firstArgType, out Type secondArgType)
        {
            firstArgType = null;
            secondArgType = null;
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name == name && m.GetParameters().Length == 2)
                .ToArray();
            if (methods.Length == 0) return null;
            var method = methods[0];
            var p0 = method.GetParameters()[0].ParameterType;
            var p1 = method.GetParameters()[1].ParameterType;
            if (!p0.IsGenericType || p0.GetGenericTypeDefinition() != typeof(Action<>)) return null;
            if (!p1.IsGenericType || p1.GetGenericTypeDefinition() != typeof(Action<>)) return null;
            firstArgType = p0.GetGenericArguments()[0];
            secondArgType = p1.GetGenericArguments()[0];
            return method;
        }

        private static Delegate CreateActionDelegate(Type responseType, Action<object> callback)
        {
            var holderType = typeof(TypedCallbackHolder<>).MakeGenericType(responseType);
            var holder = Activator.CreateInstance(holderType, callback);
            var invoke = holderType.GetMethod("Invoke");
            var delegateType = typeof(Action<>).MakeGenericType(responseType);
            return Delegate.CreateDelegate(delegateType, holder, invoke);
        }

        private class TypedCallbackHolder<T>
        {
            private readonly Action<object> _callback;
            public TypedCallbackHolder(Action<object> callback) => _callback = callback;
            public void Invoke(T response) => _callback?.Invoke(response);
        }

        private static void SetCallback(object option, string propertyName, Action<object> callback)
        {
            var prop = option.GetType().GetProperty(propertyName);
            if (prop == null) return;
            var delegateType = prop.PropertyType;
            if (!delegateType.IsGenericType || delegateType.GetGenericTypeDefinition() != typeof(Action<>))
                return;
            var responseType = delegateType.GetGenericArguments()[0];
            var del = CreateActionDelegate(responseType, callback);
            prop.SetValue(option, del);
        }

        private static void SetField(object obj, string name, object value)
        {
            var type = obj.GetType();
            var field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(obj, value);
                return;
            }
            var prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(obj, value);
            }
        }

        private static object GetFieldValue(object obj, string name)
        {
            var type = obj.GetType();
            var field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (field != null) return field.GetValue(obj);
            var prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            return prop?.GetValue(obj);
        }

        private static T GetFieldValue<T>(object obj, string name)
        {
            var value = GetFieldValue(obj, name);
            if (value == null) return default;
            return (T)Convert.ChangeType(value, typeof(T));
        }

        private static bool GetFieldValue<T>(object obj, string name, out T result)
        {
            result = default;
            try
            {
                var value = GetFieldValue(obj, name);
                if (value == null) return false;
                result = (T)Convert.ChangeType(value, typeof(T));
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}

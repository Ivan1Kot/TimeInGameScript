using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GameTimeCounter : MonoBehaviour
{
    #region Fields

    #region Enums

    private enum Platform
    {
        PC,
        Mac,
        Linux,
        Android,
        IOS
    }

    #endregion

    #region Structs

    [Serializable]
    private struct PlatformStringsStruct
    {
        public string PC;
        public string Mac;
        public string Linux;
        public string Android;
        public string IOS;
    }

    #endregion

    #region Constants

    private const string _PHP_SCRIPT_URL = "https://ivan1kot.000webhostapp.com/TimeInGameScript/";
    private const string _PHP_SCRIPT_RETURN = "request completed";

    private const string _DATABASE_HOST = "DatabaseHost";
    private const string _DATABASE_USER = "DatabaseUser";
    private const string _DATABASE_PASSWORD = "DatabasePassword";
    private const string _DATABASE_NAME = "DatabaseName";
    private const string _DATABASE_PORT_NUMBER = "DatabasePortNumber";

    private const string _GAME_TABLE = "GameTable";
    private const string _PLATFORM_COLUMN_NAME = "PlatformColumnName";
    private const string _PLATFORM = "Platform";
    private const string _TIME_COLUMN_NAME = "TimeColumnName";
    private const string _TIME_VALUE = "Time";

    private const string _STORED_TIME = "TimeInGameKey";

    #endregion

    #region Serialized Fields

    [Header("Database information")]
    [SerializeField] private string _databaseHost;
    [Space(15)]
    [SerializeField] private string _databaseUsername;
    [SerializeField] private string _databasePassword;
    [Space(15)]
    [SerializeField] private string _databaseName;
    [SerializeField] private ushort _databasePort;

    [Header("Table information")]
    [SerializeField] private string _tableName;
    [Space(15)]
    [SerializeField] private string _platfomColumnName;
    [SerializeField] private string _timeColumnName;

    [Header("Data")]
    [SerializeField] private PlatformStringsStruct _platformTableName;
    [Space(15)]
    [SerializeField] private int _dataSendingFrequency = 1;

    #endregion

    #region Private Fields

    private int _time = 0;
    private float _dataSendFrequencyInSeconds;
    private Platform _platform = Platform.PC;
    private string _currentPlatform;

    #endregion

    #endregion

    #region Methods

#region Unity Methods

    private void Awake()
    {
        #region Platform Define
#if UNITY_STANDALONE_WIN
        _platform = Platform.PC;
#endif
#if UNITY_STANDALONE_OSX
        _platform = Platform.Mac;
#endif
#if UNITY_STANDALONE_LINUX
        _platform = Platform.Linux;
#endif
#if UNITY_IOS
        _platform = Platform.IOS;
#endif
#if UNITY_ANDROID
        _platform = Platform.Android;
#endif
        #endregion

        _dataSendFrequencyInSeconds = _dataSendingFrequency * 60;

        StartCoroutine(TimeCycle());

        if (GetStoredTimeData() == 0)
            return;
        else
            _time += GetStoredTimeData();

        _currentPlatform = PlatformToStringCovertion(_platform);

        Synchronize();
    }

#endregion

#region PlayerPrefs Methods

    private static void SetTimeDataToStore(int time)
    {
        PlayerPrefs.SetInt(_STORED_TIME, time);
    }

    private static int GetStoredTimeData()
    {
        if (!PlayerPrefs.HasKey(_STORED_TIME)) return 0;
        return PlayerPrefs.GetInt(_STORED_TIME);
    }

#endregion

#region Editor Methods

#if UNITY_EDITOR
    [MenuItem("TimeInGameScript/Clean data")]
    public static void CleanTimeInGameScriptData()
    {
        PlayerPrefs.DeleteKey(_STORED_TIME);
        PlayerPrefs.Save();
        Debug.Log("Successfully clean player prefs");
    }
#endif

#endregion

#region IEnumerators

    private delegate void OnRequestComplete();
    private delegate void OnRequestError(string message);
    private IEnumerator SendTimeStatistics(string currentPlatform , int time, OnRequestComplete complete, OnRequestError error)
    {
        WWWForm newForm = new WWWForm();

        //string currentPlatform = Enum.GetName(typeof(Platform), _platform);

        newForm.AddField(_DATABASE_HOST, _databaseHost);
        newForm.AddField(_DATABASE_USER, _databaseUsername);
        newForm.AddField(_DATABASE_PASSWORD, _databasePassword);
        newForm.AddField(_DATABASE_NAME, _databaseName);
        newForm.AddField(_DATABASE_PORT_NUMBER, _databasePort);

        newForm.AddField(_GAME_TABLE, _tableName);

        newForm.AddField(_PLATFORM_COLUMN_NAME, _platfomColumnName);
        newForm.AddField(_TIME_COLUMN_NAME, _timeColumnName);
        newForm.AddField(_PLATFORM, currentPlatform);
        newForm.AddField(_TIME_VALUE, time);
        
        WWW request = new WWW(_PHP_SCRIPT_URL, newForm);
        yield return request;

        if (request.error != null)
        { 
            error("Не удалось отправить запрос на сервер");
        }
        else
        { 
            try
            {
                Debug.Log(request.text);
                if (request.text == _PHP_SCRIPT_RETURN)
                {
                    complete();
                }
                else
                {
                    error(request.text);
                }
            }
            catch
            {
                error("Не удалось обработать ответ сервера");
                Debug.Log("Ошибка обработки ответа сервера. Данные ответа: " + request.text);
            }
        }
    }


    private IEnumerator TimeCycle()
    {
        while(true)
        {
            yield return new WaitForSecondsRealtime(_dataSendFrequencyInSeconds);
            _time += _dataSendingFrequency;
            SetTimeDataToStore(_time);
            if (_time >= 5)
                Synchronize();
        }
    }

    #endregion

#region Private Methods

    private void Synchronize()
    {
        StartCoroutine(SendTimeStatistics(_currentPlatform, _time, (() =>
        {
            Debug.Log("Statistic has sended succesfully!");
            _time = 0;
            SetTimeDataToStore(_time);
        }), ((string errorMessage) =>
        {
            Debug.LogError(errorMessage);
        })));
    }

    private string PlatformToStringCovertion(Platform platform)
    {
        switch(platform)
        {
            case Platform.PC:
                return _platformTableName.PC;
                break; 
            case Platform.Mac:
                return _platformTableName.Mac;
                break;
            case Platform.Linux:
                return _platformTableName.Linux;
                break;
            case Platform.Android:
                return _platformTableName.Android;
                break;
            case Platform.IOS:
                return _platformTableName.IOS;
                break;
        }
        return "";
    }

    #endregion

    #endregion
}
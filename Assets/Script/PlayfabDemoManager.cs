using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections.Generic;
using TMPro;

public class PlayfabDemoManager : MonoBehaviour
{
    #region Login UI
    [Header("Login UI")]
    public TMP_InputField usernameInput;
    public Button loginButton;
    public TextMeshProUGUI loginStatusText;
    #endregion

    #region Currency UI
    [Header("Currency UI")]
    public TextMeshProUGUI coinText;
    public TextMeshProUGUI energyText;
    public TextMeshProUGUI otherCurrencyText;
    public Button addCoinButton;
    public Button spendCoinButton;
    #endregion

    #region Score & Leaderboard UI
    [Header("Score & Leaderboard UI")]
    public Button addScoreButton;
    public Button getLeaderboardButton;
    public RectTransform leaderboardContent;
    #endregion

    #region Quest System UI
    [Header("Quest System UI")]
    public TMP_InputField questNameInput;
    public TMP_InputField questDescriptionInput;
    public Button createQuestButton;
    public Button completeQuestButton;
    public Button loadQuestsButton;
    public TextMeshProUGUI questResultText;
    public RectTransform questListContent;
    #endregion

    #region Player Data UI
    [Header("Player Data UI")]
    public TMP_InputField dataKeyInput;
    public TMP_InputField dataValueInput;
    public Button saveDataButton;
    public Button loadDataButton;
    public TextMeshProUGUI dataResultText;
    #endregion

    #region Private Variables
    private string playfabId;
    private int coin;
    private int energy;
    private int score;
    private string displayName;
    private List<PlayerLeaderboardEntry> leaderboardEntries = new List<PlayerLeaderboardEntry>();
    private bool isLoggedIn = false;
    private Dictionary<string, QuestData> quests = new Dictionary<string, QuestData>();

    [System.Serializable]
    public class QuestData
    {
        public string name;
        public string description;
        public bool isCompleted;
        public int rewardCoin;
        public string createdAt;
    }

    // Demo: Energy tăng theo thời gian
    private float energyTimer = 0f;
    private float energyInterval = 10f;
    #endregion

    #region Unity Methods
    private void Start()
    {
        RegisterButtonListeners();
        UpdateCurrencyUI();
    }

    private void Update()
    {
        if (!isLoggedIn) return;
        energyTimer += Time.deltaTime;
        if (energyTimer >= energyInterval)
        {
            AddEnergy(1);
            energyTimer = 0f;
        }
        UpdateEnergyCountdownUI();
    }
    #endregion

    #region Button Registration
    void RegisterButtonListeners()
    {
        loginButton.onClick.AddListener(OnLoginClicked);
        addScoreButton.onClick.AddListener(OnAddScoreClicked);
        getLeaderboardButton.onClick.AddListener(OnGetLeaderboardClicked);
        addCoinButton.onClick.AddListener(OnAddCoinClicked);
        spendCoinButton.onClick.AddListener(OnSpendCoinClicked);
        //createQuestButton.onClick.AddListener(OnCreateQuestClicked);
        //completeQuestButton.onClick.AddListener(OnCompleteQuestClicked);
        //loadQuestsButton.onClick.AddListener(OnLoadQuestsClicked);
        //saveDataButton.onClick.AddListener(OnSaveDataClicked);
        //loadDataButton.onClick.AddListener(OnLoadDataClicked);
    }
    #endregion

    #region Login System
    void OnLoginClicked()
    {
        displayName = usernameInput.text;
        var request = new LoginWithCustomIDRequest
        {
            CustomId = SystemInfo.deviceUniqueIdentifier,
            CreateAccount = true
        };
        PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnLoginFailure);
        loginStatusText.text = "Đang đăng nhập...";
    }

    void OnLoginSuccess(LoginResult result)
    {
        playfabId = result.PlayFabId;
        loginStatusText.text = $"Đăng nhập thành công: {displayName}";
        isLoggedIn = true;
        UpdateDisplayName();
        GetUserInventory();
        GetUserStatistics();
        OnGetLeaderboardClicked();
    }

    void OnLoginFailure(PlayFabError error)
    {
        loginStatusText.text = "Đăng nhập thất bại: " + error.GenerateErrorReport();
    }

    void UpdateDisplayName()
    {
        var request = new UpdateUserTitleDisplayNameRequest { DisplayName = displayName };
        PlayFabClientAPI.UpdateUserTitleDisplayName(request, (r) => { }, (e) => { });
    }
    #endregion

    #region Currency System
    void GetUserInventory()
    {
        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), result =>
        {
            coin = 0;
            energy = 0;
            foreach (var pair in result.VirtualCurrency)
            {
                if (pair.Key == "CO") coin = pair.Value;
                if (pair.Key == "EN") energy = pair.Value; 
            }
            Debug.Log($"[PlayfabDemo] GetUserInventory: coin={coin}, energy={energy}");
            UpdateCurrencyUI();
        },
        error => {
            Debug.LogError($"[PlayfabDemo] GetUserInventory error: {error.GenerateErrorReport()}");
        });
    }

    void UpdateCurrencyUI()
    {
        coinText.text = $"Coin: <color=yellow>{coin}</color>";
    }

    void UpdateEnergyCountdownUI()
    {
        float timeLeft = Mathf.Ceil(energyInterval - energyTimer);
        energyText.text = $"Energy: <color=green>{energy}</color> (Cộng sau: {timeLeft}s)";
    }

    void OnAddCoinClicked()
    {
        if (!isLoggedIn) return;
        AddCoin(10);
    }

    void AddCoin(int amount)
    {
        if (!isLoggedIn) return;
        var request = new AddUserVirtualCurrencyRequest
        {
            VirtualCurrency = "CO",
            Amount = amount
        };
        PlayFabClientAPI.AddUserVirtualCurrency(request, result =>
        {
            coin += amount;
            Debug.Log($"[PlayfabDemo] AddCoin: +{amount}, new coin={coin}");
            UpdateCurrencyUI();
        }, error => {
            Debug.LogError($"[PlayfabDemo] AddCoin error: {error.GenerateErrorReport()}");
        });
    }

    void OnSpendCoinClicked()
    {
        if (!isLoggedIn) return;
        SpendCoin(5);
    }

    void SpendCoin(int amount)
    {
        if (!isLoggedIn) return;
        var request = new SubtractUserVirtualCurrencyRequest
        {
            VirtualCurrency = "CO",
            Amount = amount
        };
        PlayFabClientAPI.SubtractUserVirtualCurrency(request, result =>
        {
            coin -= amount;
            Debug.Log($"[PlayfabDemo] SpendCoin: -{amount}, new coin={coin}");
            UpdateCurrencyUI();
        }, error => {
            Debug.LogError($"[PlayfabDemo] SpendCoin error: {error.GenerateErrorReport()}");
        });
    }

    void AddEnergy(int amount)
    {
        if (!isLoggedIn) return;
        var request = new AddUserVirtualCurrencyRequest
        {
            VirtualCurrency = "EN",
            Amount = amount
        };
        PlayFabClientAPI.AddUserVirtualCurrency(request, result =>
        {
            energy += amount;
            Debug.Log($"[PlayfabDemo] AddEnergy: +{amount}, new energy={energy}");
            UpdateCurrencyUI();
        }, error => {
            Debug.LogError($"[PlayfabDemo] AddEnergy error: {error.GenerateErrorReport()}");
        });
    }
    #endregion

    #region Score & Leaderboard System
    void GetUserStatistics()
    {
        PlayFabClientAPI.GetPlayerStatistics(new GetPlayerStatisticsRequest(), result =>
        {
            foreach (var stat in result.Statistics)
            {
                if (stat.StatisticName == "Score")
                    score = stat.Value;
            }
            Debug.Log($"[PlayfabDemo] GetUserStatistics: score={score}");
        }, error => {
            Debug.LogError($"[PlayfabDemo] GetUserStatistics error: {error.GenerateErrorReport()}");
        });
    }

    void OnAddScoreClicked()
    {
        if (!isLoggedIn) return;
        score += 10;
        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate> {
                new StatisticUpdate { StatisticName = "Score", Value = score }
            }
        };
        PlayFabClientAPI.UpdatePlayerStatistics(request, result =>
        {
            loginStatusText.text = $"Score mới: {score}";
            Debug.Log($"[PlayfabDemo] AddScore: new score={score}");
            OnGetLeaderboardClicked();
        }, error =>
        {
            loginStatusText.text = "Lỗi cập nhật score: " + error.GenerateErrorReport();
            Debug.LogError($"[PlayfabDemo] AddScore error: {error.GenerateErrorReport()}");
        });
    }

    void OnGetLeaderboardClicked()
    {
        var request = new GetLeaderboardRequest
        {
            StatisticName = "Score",
            StartPosition = 0,
            MaxResultsCount = 10
        };
        PlayFabClientAPI.GetLeaderboard(request, OnLeaderboardReceived, error =>
        {
            loginStatusText.text = "Lỗi lấy leaderboard: " + error.GenerateErrorReport();
        });
    }

    void OnLeaderboardReceived(GetLeaderboardResult result)
    {
        leaderboardEntries = result.Leaderboard;
        CreateLeaderboardEntries();
    }

    void CreateLeaderboardEntries()
    {
        foreach (Transform child in leaderboardContent)
            Destroy(child.gameObject);
        
        var vLayout = leaderboardContent.GetComponent<VerticalLayoutGroup>();
        if (vLayout == null)
        {
            vLayout = leaderboardContent.gameObject.AddComponent<VerticalLayoutGroup>();
            vLayout.childAlignment = TextAnchor.UpperCenter;
            vLayout.childForceExpandHeight = false;
            vLayout.childForceExpandWidth = true;
            vLayout.childControlHeight = true;
            vLayout.childControlWidth = true;
            vLayout.spacing = 2;
            vLayout.padding = new RectOffset(0, 0, 0, 0);
        }
        
        int stt = 1;
        foreach (var entry in leaderboardEntries)
        {
            string name = entry.DisplayName ?? entry.PlayFabId;
            int score = entry.StatValue;
            CreateLeaderboardEntry(stt, name, score);
            stt++;
        }
    }

    void CreateLeaderboardEntry(int stt, string name, int score)
    {
        GameObject go = new GameObject($"Entry_{stt}", typeof(RectTransform));
        go.transform.SetParent(leaderboardContent, false);
        var entryRect = go.GetComponent<RectTransform>();
        entryRect.sizeDelta = new Vector2(0, 70);
        entryRect.anchorMin = new Vector2(0, 1);
        entryRect.anchorMax = new Vector2(1, 1);
        entryRect.pivot = new Vector2(0.5f, 1);

        var img = go.AddComponent<Image>();
        img.color = (stt % 2 == 1) ? new Color(1f, 1f, 1f, 0.25f) : new Color(0.95f, 0.95f, 0.95f, 0.5f);
        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.08f);
        outline.effectDistance = new Vector2(0, -2);

        var hLayout = go.AddComponent<HorizontalLayoutGroup>();
        hLayout.childAlignment = TextAnchor.MiddleCenter;
        hLayout.childForceExpandHeight = true;
        hLayout.childForceExpandWidth = false;
        hLayout.childControlHeight = true;
        hLayout.childControlWidth = true;
        hLayout.spacing = 16;
        hLayout.padding = new RectOffset(24, 24, 0, 0);

        float[] widths = { 80, 0, 140, 120 };
        string[] values = {
            stt.ToString(),
            name,
            score.ToString(),
            (stt == 1) ? coin.ToString() : "---"
        };
        TextAlignmentOptions[] aligns = {
            TextAlignmentOptions.Center,
            TextAlignmentOptions.Left,
            TextAlignmentOptions.Right,
            TextAlignmentOptions.Right
        };
        for (int i = 0; i < 4; i++)
        {
            var textGO = new GameObject($"Col_{i}", typeof(RectTransform));
            textGO.transform.SetParent(go.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(widths[i], 70);
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = values[i];
            tmp.fontSize = 38;
            tmp.enableAutoSizing = true;
            
            if (stt == 1)
            {
                tmp.color = new Color(0.95f, 0.6f, 0.1f);
            }
            else if (i == 2)
            {
                tmp.color = new Color(0.2f, 0.6f, 0.9f);
            }
            else if (i == 3)
            {
                tmp.color = new Color(0.9f, 0.7f, 0.1f);
            }
            else
            {
                tmp.color = new Color(0.2f, 0.2f, 0.2f);
            }
            
            tmp.alignment = aligns[i];
            tmp.margin = new Vector4(0, 0, 0, 0);
            tmp.fontStyle = (i == 2 || i == 3) ? FontStyles.Bold : FontStyles.Normal;
            var shadow = textGO.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.18f);
            shadow.effectDistance = new Vector2(1, -1);
            if (i == 1)
            {
                var layout = textGO.AddComponent<LayoutElement>();
                layout.flexibleWidth = 1;
                layout.minWidth = 140;
            }
            else
            {
                var layout = textGO.AddComponent<LayoutElement>();
                layout.preferredWidth = widths[i];
                layout.flexibleWidth = 0;
            }
        }
    }
    #endregion

    #region Quest System
    void OnCreateQuestClicked()
    {
        if (!isLoggedIn) return;
        string name = questNameInput.text;
        string description = questDescriptionInput.text;
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(description))
        {
            questResultText.text = "Vui lòng nhập đầy đủ tên và mô tả quest";
            return;
        }
        CreateQuest(name, description);
    }

    void OnCompleteQuestClicked()
    {
        if (!isLoggedIn) return;
        string name = questNameInput.text;
        if (string.IsNullOrEmpty(name))
        {
            questResultText.text = "Vui lòng nhập tên quest để hoàn thành";
            return;
        }
        CompleteQuest(name);
    }

    void OnLoadQuestsClicked()
    {
        if (!isLoggedIn) return;
        LoadQuests();
    }

    void CreateQuest(string name, string description)
    {
        var quest = new QuestData
        {
            name = name,
            description = description,
            isCompleted = false,
            rewardCoin = UnityEngine.Random.Range(10, 50),
            createdAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
        quests[name] = quest;
        SaveQuests();
        questResultText.text = $"Đã tạo quest: {name} (Thưởng: {quest.rewardCoin} coin)";
        Debug.Log($"[PlayfabDemo] CreateQuest: {name}");
    }

    void CompleteQuest(string name)
    {
        if (!quests.ContainsKey(name))
        {
            questResultText.text = $"Không tìm thấy quest: {name}";
            return;
        }
        var quest = quests[name];
        if (quest.isCompleted)
        {
            questResultText.text = $"Quest {name} đã hoàn thành rồi!";
            return;
        }
        quest.isCompleted = true;
        AddCoin(quest.rewardCoin);
        SaveQuests();
        questResultText.text = $"Hoàn thành quest: {name} (+{quest.rewardCoin} coin)";
        Debug.Log($"[PlayfabDemo] CompleteQuest: {name} (+{quest.rewardCoin} coin)");
        DisplayQuests();
    }

    void LoadQuests()
    {
        var request = new GetUserDataRequest
        {
            Keys = new List<string> { "quests" }
        };
        PlayFabClientAPI.GetUserData(request, result =>
        {
            if (result.Data.ContainsKey("quests"))
            {
                string questsJson = result.Data["quests"].Value;
                quests = JsonUtility.FromJson<Dictionary<string, QuestData>>(questsJson);
                questResultText.text = $"Đã load {quests.Count} quests";
                Debug.Log($"[PlayfabDemo] LoadQuests: {quests.Count} quests");
                DisplayQuests();
            }
            else
            {
                quests.Clear();
                questResultText.text = "Chưa có quest nào";
                Debug.Log("[PlayfabDemo] LoadQuests: No quests found");
                DisplayQuests();
            }
        }, error =>
        {
            questResultText.text = "Lỗi load quests: " + error.GenerateErrorReport();
            Debug.LogError($"[PlayfabDemo] LoadQuests error: {error.GenerateErrorReport()}");
        });
    }

    void SaveQuests()
    {
        string questsJson = JsonUtility.ToJson(quests);
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { "quests", questsJson }
            }
        };
        PlayFabClientAPI.UpdateUserData(request, result =>
        {
            Debug.Log($"[PlayfabDemo] SaveQuests: {quests.Count} quests saved");
        }, error =>
        {
            Debug.LogError($"[PlayfabDemo] SaveQuests error: {error.GenerateErrorReport()}");
        });
    }

    void DisplayQuests()
    {
        foreach (Transform child in questListContent)
            Destroy(child.gameObject);

        foreach (var kvp in quests)
        {
            var quest = kvp.Value;
            CreateQuestEntry(quest);
        }
    }

    void CreateQuestEntry(QuestData quest)
    {
        GameObject go = new GameObject($"Quest_{quest.name}", typeof(RectTransform));
        go.transform.SetParent(questListContent, false);
        var entryRect = go.GetComponent<RectTransform>();
        entryRect.sizeDelta = new Vector2(0, 80);
        entryRect.anchorMin = new Vector2(0, 1);
        entryRect.anchorMax = new Vector2(1, 1);
        entryRect.pivot = new Vector2(0.5f, 1);

        var img = go.AddComponent<Image>();
        img.color = quest.isCompleted ? new Color(0.8f, 1f, 0.8f, 0.3f) : new Color(1f, 1f, 1f, 0.2f);

        var hLayout = go.AddComponent<HorizontalLayoutGroup>();
        hLayout.childAlignment = TextAnchor.MiddleLeft;
        hLayout.childForceExpandHeight = true;
        hLayout.childForceExpandWidth = false;
        hLayout.childControlHeight = true;
        hLayout.childControlWidth = true;
        hLayout.spacing = 10;
        hLayout.padding = new RectOffset(15, 15, 5, 5);

        var infoGO = new GameObject("QuestInfo", typeof(RectTransform));
        infoGO.transform.SetParent(go.transform, false);
        var infoLayout = infoGO.AddComponent<VerticalLayoutGroup>();
        infoLayout.childAlignment = TextAnchor.MiddleLeft;
        infoLayout.childForceExpandHeight = false;
        infoLayout.childForceExpandWidth = true;
        infoLayout.childControlHeight = true;
        infoLayout.childControlWidth = true;
        infoLayout.spacing = 2;

        var nameText = CreateText(infoGO, quest.name, 24, quest.isCompleted ? Color.gray : Color.black, FontStyles.Bold);
        var descText = CreateText(infoGO, quest.description, 18, Color.gray, FontStyles.Normal);
        var rewardText = CreateText(infoGO, $"Thưởng: {quest.rewardCoin} coin", 16, new Color(0.9f, 0.7f, 0.1f), FontStyles.Normal);

        var statusGO = new GameObject("Status", typeof(RectTransform));
        statusGO.transform.SetParent(go.transform, false);
        var statusText = CreateText(statusGO, quest.isCompleted ? "✓ Hoàn thành" : "⏳ Đang làm", 20, 
            quest.isCompleted ? Color.green : Color.blue, FontStyles.Bold);
    }

    TextMeshProUGUI CreateText(GameObject parent, string text, int fontSize, Color color, FontStyles style)
    {
        var textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(parent.transform, false);
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Left;
        return tmp;
    }
    #endregion

    #region Player Data System
    void OnSaveDataClicked()
    {
        if (!isLoggedIn) return;
        string key = dataKeyInput.text;
        string value = dataValueInput.text;
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
        {
            dataResultText.text = "Vui lòng nhập đầy đủ Key và Value";
            return;
        }
        SavePlayerData(key, value);
    }

    void OnLoadDataClicked()
    {
        if (!isLoggedIn) return;
        string key = dataKeyInput.text;
        if (string.IsNullOrEmpty(key))
        {
            dataResultText.text = "Vui lòng nhập Key để load";
            return;
        }
        LoadPlayerData(key);
    }

    void SavePlayerData(string key, string value)
    {
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { key, value }
            }
        };
        PlayFabClientAPI.UpdateUserData(request, result =>
        {
            dataResultText.text = $"Đã lưu: {key} = {value}";
            Debug.Log($"[PlayfabDemo] SavePlayerData: {key} = {value}");
        }, error =>
        {
            dataResultText.text = "Lỗi lưu data: " + error.GenerateErrorReport();
            Debug.LogError($"[PlayfabDemo] SavePlayerData error: {error.GenerateErrorReport()}");
        });
    }

    void LoadPlayerData(string key)
    {
        var request = new GetUserDataRequest
        {
            Keys = new List<string> { key }
        };
        PlayFabClientAPI.GetUserData(request, result =>
        {
            if (result.Data.ContainsKey(key))
            {
                string value = result.Data[key].Value;
                dataValueInput.text = value;
                dataResultText.text = $"Đã load: {key} = {value}";
                Debug.Log($"[PlayfabDemo] LoadPlayerData: {key} = {value}");
            }
            else
            {
                dataResultText.text = $"Không tìm thấy key: {key}";
                Debug.Log($"[PlayfabDemo] LoadPlayerData: Key {key} not found");
            }
        }, error =>
        {
            dataResultText.text = "Lỗi load data: " + error.GenerateErrorReport();
            Debug.LogError($"[PlayfabDemo] LoadPlayerData error: {error.GenerateErrorReport()}");
        });
    }
    #endregion
} 
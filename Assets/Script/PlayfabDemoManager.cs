using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections.Generic;
using TMPro; // Thêm using cho TextMeshPro

public class PlayfabDemoManager : MonoBehaviour
{
    [Header("Login UI")]
    public TMP_InputField usernameInput; // Đổi sang TMP_InputField
    public Button loginButton;
    public TextMeshProUGUI loginStatusText;

    [Header("Currency UI")]
    public TextMeshProUGUI coinText; // vàng
    public TextMeshProUGUI energyText; // xanh
    public Button addCoinButton; // Thêm nút cộng coin

    [Header("Score & Leaderboard UI")]
    public Button addScoreButton;
    public Button getLeaderboardButton;
    public RectTransform leaderboardContent; // Đổi từ Transform sang RectTransform
    // public GameObject leaderboardEntryPrefab; // Không cần nữa

    private string playfabId;
    private int coin;
    private int energy;
    private int score;
    private string displayName;
    private List<PlayerLeaderboardEntry> leaderboardEntries = new List<PlayerLeaderboardEntry>();
    private bool isLoggedIn = false;

    private void Start()
    {
        loginButton.onClick.AddListener(OnLoginClicked);
        addScoreButton.onClick.AddListener(OnAddScoreClicked);
        getLeaderboardButton.onClick.AddListener(OnGetLeaderboardClicked);
        addCoinButton.onClick.AddListener(OnAddCoinClicked); // Đăng ký sự kiện
        UpdateCurrencyUI();
    }

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
        isLoggedIn = true; // Đánh dấu đã đăng nhập
        UpdateDisplayName();
        GetUserInventory();
        GetUserStatistics();
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

    void UpdateCurrencyUI()
    {
        coinText.text = $"Coin: <color=yellow>{coin}</color>";
        // energyText sẽ được cập nhật bởi UpdateEnergyCountdownUI
        // otherCurrencyText.text = ... nếu cần thêm loại khác
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
        // Tạo GameObject entry
        GameObject go = new GameObject($"Entry_{stt}", typeof(RectTransform));
        go.transform.SetParent(leaderboardContent, false);
        var entryRect = go.GetComponent<RectTransform>();
        entryRect.sizeDelta = new Vector2(0, 40); // Chiều cao entry, chiều rộng tự động
        entryRect.anchorMin = new Vector2(0, 1);
        entryRect.anchorMax = new Vector2(1, 1);
        entryRect.pivot = new Vector2(0.5f, 1);

        // Thêm Image background để entry có kích thước
        var img = go.AddComponent<Image>();
        img.color = new Color(1, 1, 1, 0.1f); // Màu nền nhạt

        // Tạo 3 TextMeshProUGUI: STT, Name, Score
        float[] widths = { 60, 200, 100 }; // width cho từng cột
        string[] values = {
            stt.ToString(),
            name,
            score.ToString()
        };
        for (int i = 0; i < 3; i++)
        {
            var textGO = new GameObject($"Col_{i}", typeof(RectTransform));
            textGO.transform.SetParent(go.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(widths[i], 40);
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(0, 1);
            textRect.pivot = new Vector2(0, 0.5f);
            textRect.anchoredPosition = new Vector2(i == 0 ? 0 : widths[0] + (i - 1) * widths[1], 0);

            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = values[i];
            tmp.fontSize = 24;
            tmp.enableAutoSizing = true;
            tmp.color = Color.black;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            if (i == 0) tmp.alignment = TextAlignmentOptions.Center;
            if (i == 2) tmp.alignment = TextAlignmentOptions.Right;
        }
    }

    // Demo: Energy tăng theo thời gian
    private float energyTimer = 0f;
    private float energyInterval = 10f; // mỗi 10s cộng 1 energy
    private void Update()
    {
        if (!isLoggedIn) return; // Chưa đăng nhập thì không làm gì cả
        energyTimer += Time.deltaTime;
        if (energyTimer >= energyInterval)
        {
            AddEnergy(1);
            energyTimer = 0f;
        }
        UpdateEnergyCountdownUI();
    }

    void UpdateEnergyCountdownUI()
    {
        float timeLeft = Mathf.Ceil(energyInterval - energyTimer);
        energyText.text = $"Energy: <color=green>{energy}</color> (Cộng sau: {timeLeft}s)";
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

    void OnAddCoinClicked()
    {
        if (!isLoggedIn) return;
        AddCoin(10); // Cộng 10 coin mỗi lần bấm
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
} 
using Microsoft.Extensions.AI;
using OllamaSharp;

namespace ChatAppAI
{
    public partial class Form1 : Form
    {
        // ローカルで起動している Ollama サーバーに接続するチャットクライアント。
        // OllamaApiClient は Microsoft.Extensions.AI の IChatClient を実装している。
        // 使用するモデルは起動時に取得した一覧から cmbModel で選択する。
        private readonly OllamaApiClient _chatClient =
            new(new Uri("http://127.0.0.1:11434"));

        public Form1()
        {
            InitializeComponent();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            await LoadModelsAsync();
        }

        // Ollama にインストール済みのローカルモデル一覧を取得し、ドロップダウンに反映する。
        private async Task LoadModelsAsync()
        {
            try
            {
                var models = await _chatClient.ListLocalModelsAsync();
                var names = models
                    .Select(m => m.Name)
                    .OrderBy(name => name)
                    .ToArray();

                cmbModel.Items.Clear();
                cmbModel.Items.AddRange(names);

                if (cmbModel.Items.Count > 0)
                {
                    // 先頭のモデルを既定として選択する（SelectedIndexChanged で SelectedModel が設定される）。
                    cmbModel.SelectedIndex = 0;
                }
                else
                {
                    txbReceiveMessage.Text =
                        "利用可能なモデルが見つかりませんでした。Ollama が起動しているか、モデルが pull 済みか確認してください。";
                }
            }
            catch (Exception ex)
            {
                txbReceiveMessage.Text = $"モデル一覧の取得に失敗しました: {ex.Message}";
            }
        }

        // ドロップダウンで選択されたモデルをクライアントに反映する。
        private void cmbModel_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbModel.SelectedItem is string model)
            {
                _chatClient.SelectedModel = model;
                Text = $"ChatAppAI - {model}";
            }
        }

        private async void btnSend_Click(object sender, EventArgs e)
        {
            var message = txbSendMessage.Text;
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            // モデルが未選択のまま送信されないようにする。
            if (string.IsNullOrEmpty(_chatClient.SelectedModel))
            {
                txbReceiveMessage.Text = "モデルを選択してください。";
                return;
            }

            // 送信中は二重送信を防ぐためボタンを無効化する。
            btnSend.Enabled = false;
            txbReceiveMessage.Clear();

            try
            {
                // txbSendMessage の内容を選択中のモデルに送信し、応答をストリーミングで受信する。
                await foreach (var update in _chatClient.GetStreamingResponseAsync(message))
                {
                    // 届いた断片を都度 txbReceiveMessage に追記して表示する。
                    txbReceiveMessage.AppendText(update.Text);
                }
            }
            catch (Exception ex)
            {
                txbReceiveMessage.Text = $"エラー: {ex.Message}";
            }
            finally
            {
                btnSend.Enabled = true;
            }
        }
    }
}

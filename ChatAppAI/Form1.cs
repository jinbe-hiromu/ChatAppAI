using Microsoft.Extensions.AI;
using OllamaSharp;

namespace ChatAppAI
{
    public partial class Form1 : Form
    {
        // ローカルで起動している Ollama の gemma3:4b に接続するチャットクライアント。
        // OllamaApiClient は Microsoft.Extensions.AI の IChatClient を実装している。
        private readonly IChatClient _chatClient =
            new OllamaApiClient(new Uri("http://localhost:11434"), "gemma3:4b");

        public Form1()
        {
            InitializeComponent();
        }

        private async void btnSend_Click(object sender, EventArgs e)
        {
            var message = txbSendMessage.Text;
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            // 送信中は二重送信を防ぐためボタンを無効化する。
            btnSend.Enabled = false;
            txbReceiveMessage.Clear();

            try
            {
                // txbSendMessage の内容を gemma3 に送信し、応答をストリーミングで受信する。
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

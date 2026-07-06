namespace ChatAppAI
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            lblSendMessage = new Label();
            txbSendMessage = new TextBox();
            btnSend = new Button();
            lblReceiveMessage = new Label();
            txbReceiveMessage = new TextBox();
            SuspendLayout();
            //
            // lblSendMessage
            //
            lblSendMessage.AutoSize = true;
            lblSendMessage.Location = new Point(12, 9);
            lblSendMessage.Name = "lblSendMessage";
            lblSendMessage.Size = new Size(76, 15);
            lblSendMessage.TabIndex = 0;
            lblSendMessage.Text = "送信メッセージ";
            //
            // txbSendMessage
            //
            txbSendMessage.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txbSendMessage.Location = new Point(12, 27);
            txbSendMessage.Multiline = true;
            txbSendMessage.Name = "txbSendMessage";
            txbSendMessage.ScrollBars = ScrollBars.Vertical;
            txbSendMessage.Size = new Size(776, 120);
            txbSendMessage.TabIndex = 1;
            //
            // btnSend
            //
            btnSend.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSend.Location = new Point(668, 153);
            btnSend.Name = "btnSend";
            btnSend.Size = new Size(120, 35);
            btnSend.TabIndex = 2;
            btnSend.Text = "送信";
            btnSend.UseVisualStyleBackColor = true;
            btnSend.Click += btnSend_Click;
            //
            // lblReceiveMessage
            //
            lblReceiveMessage.AutoSize = true;
            lblReceiveMessage.Location = new Point(12, 200);
            lblReceiveMessage.Name = "lblReceiveMessage";
            lblReceiveMessage.Size = new Size(76, 15);
            lblReceiveMessage.TabIndex = 3;
            lblReceiveMessage.Text = "受信メッセージ";
            //
            // txbReceiveMessage
            //
            txbReceiveMessage.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txbReceiveMessage.Location = new Point(12, 218);
            txbReceiveMessage.Multiline = true;
            txbReceiveMessage.Name = "txbReceiveMessage";
            txbReceiveMessage.ReadOnly = true;
            txbReceiveMessage.ScrollBars = ScrollBars.Vertical;
            txbReceiveMessage.Size = new Size(776, 220);
            txbReceiveMessage.TabIndex = 4;
            //
            // Form1
            //
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(txbReceiveMessage);
            Controls.Add(lblReceiveMessage);
            Controls.Add(btnSend);
            Controls.Add(txbSendMessage);
            Controls.Add(lblSendMessage);
            Name = "Form1";
            Text = "ChatAppAI - gemma3:4b";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblSendMessage;
        private TextBox txbSendMessage;
        private Button btnSend;
        private Label lblReceiveMessage;
        private TextBox txbReceiveMessage;
    }
}

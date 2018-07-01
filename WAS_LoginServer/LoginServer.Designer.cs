namespace WAS_LoginServer
{
    partial class frmLoginServer
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblConnectedClients = new System.Windows.Forms.Label();
            this.lbxClients = new System.Windows.Forms.ListBox();
            this.txbLog = new System.Windows.Forms.TextBox();
            this.txbMessage = new System.Windows.Forms.TextBox();
            this.btnToSelected = new System.Windows.Forms.Button();
            this.btnSendToAll = new System.Windows.Forms.Button();
            this.btnSendCommand = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblStatus.Location = new System.Drawing.Point(13, 13);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.lblStatus.Size = new System.Drawing.Size(73, 25);
            this.lblStatus.TabIndex = 0;
            this.lblStatus.Text = "Status";
            // 
            // lblConnectedClients
            // 
            this.lblConnectedClients.AutoSize = true;
            this.lblConnectedClients.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblConnectedClients.Location = new System.Drawing.Point(13, 49);
            this.lblConnectedClients.Name = "lblConnectedClients";
            this.lblConnectedClients.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.lblConnectedClients.Size = new System.Drawing.Size(212, 25);
            this.lblConnectedClients.TabIndex = 1;
            this.lblConnectedClients.Text = "Connected Clients: 0";
            // 
            // lbxClients
            // 
            this.lbxClients.FormattingEnabled = true;
            this.lbxClients.Location = new System.Drawing.Point(13, 78);
            this.lbxClients.Name = "lbxClients";
            this.lbxClients.Size = new System.Drawing.Size(108, 355);
            this.lbxClients.TabIndex = 2;
            // 
            // txbLog
            // 
            this.txbLog.BackColor = System.Drawing.SystemColors.HighlightText;
            this.txbLog.Location = new System.Drawing.Point(127, 78);
            this.txbLog.Multiline = true;
            this.txbLog.Name = "txbLog";
            this.txbLog.ReadOnly = true;
            this.txbLog.Size = new System.Drawing.Size(445, 276);
            this.txbLog.TabIndex = 3;
            this.txbLog.WordWrap = false;
            // 
            // txbMessage
            // 
            this.txbMessage.Location = new System.Drawing.Point(128, 381);
            this.txbMessage.Name = "txbMessage";
            this.txbMessage.Size = new System.Drawing.Size(444, 20);
            this.txbMessage.TabIndex = 4;
            // 
            // btnToSelected
            // 
            this.btnToSelected.Location = new System.Drawing.Point(128, 407);
            this.btnToSelected.Name = "btnToSelected";
            this.btnToSelected.Size = new System.Drawing.Size(109, 23);
            this.btnToSelected.TabIndex = 5;
            this.btnToSelected.Text = "Send to selected";
            this.btnToSelected.UseVisualStyleBackColor = true;
            // 
            // btnSendToAll
            // 
            this.btnSendToAll.Location = new System.Drawing.Point(243, 407);
            this.btnSendToAll.Name = "btnSendToAll";
            this.btnSendToAll.Size = new System.Drawing.Size(110, 23);
            this.btnSendToAll.TabIndex = 6;
            this.btnSendToAll.Text = "Send to All";
            this.btnSendToAll.UseVisualStyleBackColor = true;
            // 
            // btnSendCommand
            // 
            this.btnSendCommand.Location = new System.Drawing.Point(462, 407);
            this.btnSendCommand.Name = "btnSendCommand";
            this.btnSendCommand.Size = new System.Drawing.Size(110, 23);
            this.btnSendCommand.TabIndex = 7;
            this.btnSendCommand.Text = "Send Command";
            this.btnSendCommand.UseVisualStyleBackColor = true;
            // 
            // frmLoginServer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 442);
            this.Controls.Add(this.btnSendCommand);
            this.Controls.Add(this.btnSendToAll);
            this.Controls.Add(this.btnToSelected);
            this.Controls.Add(this.txbMessage);
            this.Controls.Add(this.txbLog);
            this.Controls.Add(this.lbxClients);
            this.Controls.Add(this.lblConnectedClients);
            this.Controls.Add(this.lblStatus);
            this.Name = "frmLoginServer";
            this.Text = "Login Server";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblConnectedClients;
        private System.Windows.Forms.ListBox lbxClients;
        private System.Windows.Forms.TextBox txbLog;
        private System.Windows.Forms.TextBox txbMessage;
        private System.Windows.Forms.Button btnToSelected;
        private System.Windows.Forms.Button btnSendToAll;
        private System.Windows.Forms.Button btnSendCommand;
    }
}


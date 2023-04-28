namespace Monte_Carlo_based_Pi_Calculator_Form
{
    partial class UnosiDecimale
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            textBox1 = new TextBox();
            label1 = new Label();
            label2 = new Label();
            button1 = new Button();
            label3 = new Label();
            label4 = new Label();
            SuspendLayout();
            // 
            // textBox1
            // 
            textBox1.Location = new Point(30, 71);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(125, 27);
            textBox1.TabIndex = 0;
            textBox1.TextChanged += textBox1_TextChanged;
            textBox1.KeyDown += textBox1_KeyDown;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(30, 31);
            label1.Name = "label1";
            label1.Size = new Size(101, 20);
            label1.TabIndex = 1;
            label1.Text = "Broj decimala";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(30, 124);
            label2.Name = "label2";
            label2.Size = new Size(325, 20);
            label2.TabIndex = 2;
            label2.Text = "                                                                               ";
            // 
            // button1
            // 
            button1.Location = new Point(30, 221);
            button1.Name = "button1";
            button1.Size = new Size(127, 29);
            button1.TabIndex = 3;
            button1.Text = "Druga opcija";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(30, 154);
            label3.Name = "label3";
            label3.Size = new Size(325, 20);
            label3.TabIndex = 4;
            label3.Text = "                                                                               ";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(30, 183);
            label4.Name = "label4";
            label4.Size = new Size(325, 20);
            label4.TabIndex = 5;
            label4.Text = "                                                                               ";
            // 
            // UnosiDecimale
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(366, 277);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(button1);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(textBox1);
            Name = "UnosiDecimale";
            Text = "UnosiDecimalacs";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox textBox1;
        private Label label1;
        private Label label2;
        private Button button1;
        private Label label3;
        private Label label4;
    }
}
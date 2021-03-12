using PeNet.Header.Pe;
using Iced.Intel;
using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DiffAsm
{
    public partial class Form1 : Form
    {
        const int WM_USER = 0x400;
        const int EM_GETSCROLLPOS = WM_USER + 221;
        const int EM_SETSCROLLPOS = WM_USER + 222;
        public bool done = false;
        double pbUnit;
        int pbWIDTH, pbHEIGHT, pbComplete;
        Bitmap bmp;
        Graphics g;


        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern int SendMessage(IntPtr hWnd, int msg, int wParam, ref Point lParam);

        private void richTextBoxOriginal_VScroll(object sender, EventArgs e)
        {
            Point pt = new Point();
            if (done)
            {
                SendMessage(richTextBoxOriginal.Handle, EM_GETSCROLLPOS, 0, ref pt);
                SendMessage(richTextBoxPatched.Handle, EM_SETSCROLLPOS, 0, ref pt);
            }
        }

        private void richTextBoxPatched_VScroll(object sender, EventArgs e)
        {
            Point pt = new Point();
            if (done)
            {
                SendMessage(richTextBoxPatched.Handle, EM_GETSCROLLPOS, 0, ref pt);
                SendMessage(richTextBoxOriginal.Handle, EM_SETSCROLLPOS, 0, ref pt);
            }
        }

        public Form1()
        {
            InitializeComponent();
        }


        private void buttonDiff_Click(object sender, EventArgs e)
        {
            richTextBoxOriginal.Clear();
            richTextBoxPatched.Clear();

            g.Clear(Color.MintCream);
            picboxPB.Image = bmp;

            done = false;

            int nbdiff = 0;
            int curline = 0;
            bool equal = true;
            int nb_inst_O = 0;
            int nb_inst_P = 0;

            var codebyteOriginal = Disamexe(textBoxOriginal.Text);
            var codebytePatched = Disamexe(textBoxPatched.Text);
            var instructionsOriginal = codebyteOriginal.instructions;
            var instructionsPatched = codebytePatched.instructions;

            var formatter = new MasmFormatter();
            formatter.Options.DigitSeparator = "";
            formatter.Options.FirstOperandCharIndex = 10;
            var outputO = new StringOutput();
            var outputP = new StringOutput();

            progressBar1.Maximum = instructionsOriginal.Count();
            richTextBoxOriginal.BackColor = Color.MintCream;
            richTextBoxOriginal.ForeColor = Color.DarkGreen;
            richTextBoxPatched.BackColor = Color.MintCream;
            richTextBoxPatched.ForeColor = Color.DarkGreen;

            while (nb_inst_O < instructionsOriginal.Count() && nb_inst_P < instructionsPatched.Count())
            {
                var instrO = instructionsOriginal[nb_inst_O];
                var instrP = instructionsPatched[nb_inst_P];

                while  ((instrO.ToString() != instrP.ToString()) || (instrO.IP != instrP.IP))
                {
                    if (equal)
                    {
                        AffRich(instructionsOriginal[nb_inst_O-1], richTextBoxOriginal, outputO, codebyteOriginal.hexcode, codebyteOriginal.CodeRIP);
                        AffRich(instructionsPatched[nb_inst_P-1], richTextBoxPatched, outputP, codebytePatched.hexcode, codebytePatched.CodeRIP);
                        curline++;
                        equal = false;
                        RedProgess(((float)nb_inst_O / (float)instructionsOriginal.Count() * 100));
                    }
                    AffRich(instructionsOriginal[nb_inst_O], richTextBoxOriginal, outputO, codebyteOriginal.hexcode, codebyteOriginal.CodeRIP);
                    AffRich(instructionsPatched[nb_inst_P], richTextBoxPatched, outputP, codebytePatched.hexcode, codebytePatched.CodeRIP);
                    ModifColor(richTextBoxOriginal, curline);
                    ModifColor(richTextBoxPatched, curline);

                    curline++;
                    nb_inst_O++;
                    nb_inst_P++;
                    instrO = instructionsOriginal[nb_inst_O];
                    instrP = instructionsPatched[nb_inst_P];

                    while (instrO.IP < instrP.IP)
                    {
                        AffRich(instructionsOriginal[nb_inst_O], richTextBoxOriginal, outputO, codebyteOriginal.hexcode, codebyteOriginal.CodeRIP);
                        richTextBoxPatched.AppendText(Environment.NewLine);
                        ModifColor(richTextBoxOriginal, curline);

                        curline++;
                        nb_inst_O++;
                        instrO = instructionsOriginal[nb_inst_O];
                    }
                    while (instrO.IP > instrP.IP)
                    {
                        AffRich(instructionsPatched[nb_inst_P], richTextBoxPatched, outputP, codebytePatched.hexcode, codebytePatched.CodeRIP);
                        richTextBoxOriginal.AppendText(Environment.NewLine);
                        ModifColor(richTextBoxPatched, curline);

                        curline++;
                        nb_inst_P++;
                        instrP = instructionsPatched[nb_inst_P];
                    }
                    nbdiff++;
                }

                if (!equal)
                {
                    AffRich(instructionsOriginal[nb_inst_O], richTextBoxOriginal, outputO, codebyteOriginal.hexcode, codebyteOriginal.CodeRIP);
                    AffRich(instructionsPatched[nb_inst_P], richTextBoxPatched, outputP, codebytePatched.hexcode, codebytePatched.CodeRIP);
                    curline++;
                    richTextBoxOriginal.AppendText("----------------------------------------------------" + Environment.NewLine);
                    richTextBoxPatched.AppendText("----------------------------------------------------" + Environment.NewLine);
                    curline++;
                    equal = true;
                }

                nb_inst_O++;
                nb_inst_P++;

                progressBar1.Value = nb_inst_O; 
            }
            progressBar1.Value = 0;
            done = true;
        }

        private void RedProgess(float percent)
        {
            pbComplete = (int)Math.Round((decimal)percent);
            g.FillRectangle(Brushes.PaleVioletRed, new Rectangle((int)(Math.Floor(pbComplete * pbUnit)), 0, (int)(Math.Round(pbUnit)), pbHEIGHT));
            picboxPB.Image = bmp;
        }

        private void ModifColor(RichTextBox richTextBox, int curline)
        {
            richTextBox.Select(richTextBox.GetFirstCharIndexFromLine(curline), richTextBox.Lines[curline].Length);
            richTextBox.SelectionColor = Color.Crimson;
            richTextBox.SelectionBackColor = Color.LavenderBlush;
        }

        private void AffRich(Instruction instruction, RichTextBox richTextBox, StringOutput stringOutput, byte[] buffer, ulong CodeRIP)
        {
            const int HEXBYTES_COLUMN_BYTE_LENGTH = 10;

            var formatter = new MasmFormatter();
            formatter.Format(instruction, stringOutput);
            richTextBox.AppendText(instruction.IP.ToString("X16"));
            richTextBox.AppendText(" ");
            int instrLen = instruction.Length;
            int byteBaseIndex = (int)(instruction.IP - CodeRIP);
            for (int i = 0; i < instrLen; i++)
                richTextBox.AppendText(buffer[byteBaseIndex + i].ToString("X2"));
            
            int missingBytes = HEXBYTES_COLUMN_BYTE_LENGTH - instrLen;
            for (int i = 0; i < missingBytes; i++)
                richTextBox.AppendText("  ");

            richTextBox.AppendText(" ");
            string endasm = stringOutput.ToStringAndReset().PadRight(60);
            richTextBox.AppendText(endasm + Environment.NewLine);
        }
        private class CodeByte
        {
            public InstructionList instructions;
            public byte[] hexcode;
            public ulong CodeRIP;
        }

        private CodeByte Disamexe(string fileexe)
        {
            progressBar1.Maximum = 100;
            CodeByte codeByte = new CodeByte();
            
            int exampleCodeBitness;
            var peHeader = new PeNet.PeFile(fileexe);
            if (peHeader.Is64Bit) { exampleCodeBitness = 64; } else { exampleCodeBitness = 32; }

            FileStream input = new FileStream(fileexe, FileMode.Open, FileAccess.Read);
            BinaryReader reader = new BinaryReader(input);
            reader.ReadBytes((int)peHeader.ImageSectionHeaders[0].PointerToRawData);
            byte[] buffer = reader.ReadBytes((int)peHeader.ImageSectionHeaders[0].SizeOfRawData);
            input.Close();

            ulong exampleCodeRIP = peHeader.ImageNtHeaders.OptionalHeader.ImageBase + peHeader.ImageSectionHeaders[0].VirtualAddress;
            var codeBytes = buffer;
            var codeReader = new ByteArrayCodeReader(codeBytes);
            var decoder = Iced.Intel.Decoder.Create(exampleCodeBitness, codeReader);
            decoder.IP = exampleCodeRIP;
            ulong endRip = decoder.IP + (uint)codeBytes.Length;
            var instructions = new InstructionList();
            while (decoder.IP < endRip)
            {
                decoder.Decode(out instructions.AllocUninitializedElement());
                progressBar1.Value = (int)(decoder.IP * 100 / endRip);
            }
            codeByte.instructions = instructions;
            codeByte.hexcode = buffer;
            codeByte.CodeRIP = exampleCodeRIP;

            return codeByte;
        }

            private void button1_Click(object sender, EventArgs e)
        {
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == DialogResult.OK) // Test result.
            {
                textBoxOriginal.Text = openFileDialog1.FileName;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == DialogResult.OK) // Test result.
            {
                textBoxPatched.Text = openFileDialog1.FileName;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            pbWIDTH = picboxPB.Width;
            pbHEIGHT = picboxPB.Height;
            pbUnit = pbWIDTH / 100.0;
            //pbComplete - This is equal to work completed in % [min = 0 max = 100]
            pbComplete = 0;
            //create bitmap
            bmp = new Bitmap(pbWIDTH, pbHEIGHT);
            g = Graphics.FromImage(bmp);
            g.Clear(Color.MintCream);
            picboxPB.Image = bmp;

        }
    }
}

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
        public Form1()
        {
            InitializeComponent();
        }

        private void buttonDiff_Click(object sender, EventArgs e)
        {
            var instructionsOriginal = Disamexe(textBoxOriginal.Text);
            var instructionsPatched = Disamexe(textBoxPatched.Text);
            var formatter = new MasmFormatter();
            formatter.Options.DigitSeparator = "";
            formatter.Options.FirstOperandCharIndex = 10;
            var outputO = new StringOutput();
            var outputP = new StringOutput();

            progressBar1.Maximum = instructionsOriginal.Count();
            richTextBoxOriginal.BackColor = Color.AliceBlue;
            richTextBoxOriginal.ForeColor = Color.DarkBlue;
            richTextBoxPatched.BackColor = Color.AliceBlue;
            richTextBoxPatched.ForeColor = Color.DarkBlue;
            int nbdiff = 0;
            int curline = 0;
            bool equal = true;
            int nb_inst_O = 0; 
            int nb_inst_P = 0;
            while (nb_inst_O < instructionsOriginal.Count() && nb_inst_P < instructionsPatched.Count())
            {
                var instrO = instructionsOriginal[nb_inst_O];
                var instrP = instructionsPatched[nb_inst_P];

                while  ((instrO.ToString() != instrP.ToString()) || (instrO.IP != instrP.IP))
                {
                    if (equal)
                    {
                        AffRich(instructionsOriginal[nb_inst_O-1], richTextBoxOriginal, outputO);
                        AffRich(instructionsPatched[nb_inst_P-1], richTextBoxPatched, outputP);
                        curline++;
                        equal = false;
                     }
                    AffRich(instructionsOriginal[nb_inst_O], richTextBoxOriginal, outputO);
                    AffRich(instructionsPatched[nb_inst_P], richTextBoxPatched, outputP);

                    richTextBoxOriginal.Select(richTextBoxOriginal.GetFirstCharIndexFromLine(curline), richTextBoxOriginal.Lines[curline].Length);
                    richTextBoxOriginal.SelectionColor = Color.Crimson;
                    richTextBoxOriginal.SelectionBackColor = Color.LavenderBlush;
                    richTextBoxPatched.Select(richTextBoxPatched.GetFirstCharIndexFromLine(curline), richTextBoxPatched.Lines[curline].Length);
                    richTextBoxPatched.SelectionColor = Color.Crimson;
                    richTextBoxPatched.SelectionBackColor = Color.LavenderBlush;

                    curline++;
                    nb_inst_O++;
                    nb_inst_P++;
                    instrO = instructionsOriginal[nb_inst_O];
                    instrP = instructionsPatched[nb_inst_P];

                    while (instrO.IP < instrP.IP)
                    {
                        AffRich(instructionsOriginal[nb_inst_O], richTextBoxOriginal, outputO);
                        richTextBoxPatched.AppendText(Environment.NewLine);
                        richTextBoxOriginal.Select(richTextBoxOriginal.GetFirstCharIndexFromLine(curline), richTextBoxOriginal.Lines[curline].Length);
                        richTextBoxOriginal.SelectionColor = Color.Crimson;
                        richTextBoxOriginal.SelectionBackColor = Color.LavenderBlush;

                        curline++;
                        nb_inst_O++;
                        instrO = instructionsOriginal[nb_inst_O];
                    }
                    while (instrO.IP > instrP.IP)
                    {
                        AffRich(instructionsPatched[nb_inst_P], richTextBoxPatched, outputP);
                        richTextBoxOriginal.AppendText(Environment.NewLine);
                        richTextBoxPatched.Select(richTextBoxPatched.GetFirstCharIndexFromLine(curline), richTextBoxPatched.Lines[curline].Length);
                        richTextBoxPatched.SelectionColor = Color.Crimson;
                        richTextBoxPatched.SelectionBackColor = Color.LavenderBlush;

                        curline++;
                        nb_inst_P++;
                        instrP = instructionsPatched[nb_inst_P];
                    }

                    nbdiff++;

                }

                if (!equal)
                {
                    AffRich(instructionsOriginal[nb_inst_O], richTextBoxOriginal, outputO);
                    AffRich(instructionsPatched[nb_inst_P], richTextBoxPatched, outputP);
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
        }
        private void AffRich(Instruction instruction, RichTextBox richTextBox, StringOutput stringOutput)
        {
            var formatter = new MasmFormatter();
            formatter.Format(instruction, stringOutput);
            richTextBox.AppendText(instruction.IP.ToString("X16"));
            richTextBox.AppendText(" ");

            richTextBox.AppendText(" ");
            string endasm = stringOutput.ToStringAndReset().PadRight(60);
            richTextBox.AppendText(endasm + Environment.NewLine);
        }

        private InstructionList Disamexe(string fileexe)
        {
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
            }

            return instructions;
        }
        private void Asmtorichbox(InstructionList instructions, RichTextBox richTextBox)
        {
            var formatter = new MasmFormatter();
            formatter.Options.DigitSeparator = "";
            formatter.Options.FirstOperandCharIndex = 10;
            var output = new StringOutput();

            foreach (ref var instr in instructions)
            {
                formatter.Format(instr, output);
                //richTextBox.BackColor = Color.AliceBlue;
                //richTextBox.ForeColor = Color.Aqua;
                richTextBox.AppendText(instr.IP.ToString("X16"));
                richTextBox.AppendText(" ");
                //int instrLen = instr.Length;
                //int byteBaseIndex = (int)(instr.IP - exampleCodeRIP);
                //for (int i = 0; i < instrLen; i++)
                //    richTextBox.AppendText(codeBytes[byteBaseIndex + i].ToString("X2"));
                //int missingBytes = HEXBYTES_COLUMN_BYTE_LENGTH - instrLen;
                //for (int i = 0; i < missingBytes; i++)
                //    richTextBox.AppendText("  ");
                richTextBox.AppendText(" ");
                richTextBox.AppendText(output.ToStringAndReset() + Environment.NewLine);

            }
        }

            private void button1_Click(object sender, EventArgs e)
        {
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == DialogResult.OK) // Test result.
            {
                textBoxOriginal.Text = openFileDialog1.FileName;
                //var instructionsOriginal = Disamexe(textBoxOriginal.Text);
                //Asmtorichbox(instructionsOriginal,richTextBoxOriginal);
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == DialogResult.OK) // Test result.
            {
                textBoxPatched.Text = openFileDialog1.FileName;
                //var instructionsPatched = Disamexe(textBoxPatched.Text);
                //Asmtorichbox(instructionsPatched, richTextBoxPatched);
            }

        }
    }
}

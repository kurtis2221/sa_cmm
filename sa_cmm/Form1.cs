using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using MemoryEdit;

namespace sa_cmm
{
    public partial class Form1 : Form
    {
        const string modinfo_file = "modinfo.txt";

        string[] tmp;

        string[] files =
        {
            "american.gxt",
            "carcols.dat",
            "effects.fxp",
            "fonts.txd",
            "fronten2.txd",
            "fronten_pc.txd",
            "gta.dat",
            "handling.cfg",
            "hud.txd",
            "LOADSCS.txd",
            "main.scm", //split
            "main.scm", //fulldir
            "object.dat",
            "particle.txd",
            "default.dat",
            "weapon.dat"
        };

        uint[,] pointers =
        {
            {0x69FCE1, 0x6A01BE}, //american.gxt and spanish.gxt
            {0x5B68A0, 0x0}, //carcols.dat
            {0x49EA9D, 0x0}, //effects.fxp and effectsPC.txd
            {0x5BA69E, 0x0}, //fonts.txd
            {0x57303A, 0x57313C}, //fronten2.txd
            {0x572FAF, 0x0}, //fronten_pc.txd
            {0x53DF1F, 0x53E580}, //gta.dat
            {0x5BD838, 0x5BD84B}, //handling.cfg
            {0x5BA85F, 0x0}, //hud.txd
            {0x5900B6, 0x5900CC}, //LOADSCS.txd
            {0x468EB5, 0x468EC4}, //main.scm split
            {0x489A45, 0x0}, //main.scm full
            {0x5B925A, 0x0}, //object.dat
            {0x5BF8B1, 0x0}, //particle.txd
            {0x53BC90, 0x0}, //default.dat
            {0x5BE685, 0x0} //weapon.dat
        };

        bool[,] prop =
        {
            //1 address | is same
            {false, true}, //american.gxt and spanish.gxt
            {true, false}, //carcols.dat
            {true, false}, //effects.fxp and effectsPC.txd
            {true, false}, //fonts.txd
            {true, true}, //fronten2.txd
            {true, false}, //fronten_pc.txd
            {true, true}, //gta.dat
            {false, false}, //handling.cfg
            {true, false}, //hud.txd
            {false, false}, //LOADSCS.txd
            {false, false}, //main.scm split
            {true, false}, //main.scm full
            {true, false}, //object.dat
            {true, false}, //particle.txd
            {true, false}, //default.dat
            {true, false} //weapon.dat
        };

        uint tmpaddr = 0x0;
        uint incaddr = 0x0;

        uint mainaddr1 = 0x0;
        uint mainaddr2 = 0x0;
        string folder = null;

        StreamReader sr;
        Memory mem;
        ASCIIEncoding encoder = new ASCIIEncoding();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (Directory.Exists("mods"))
            {
                tmp = Directory.GetDirectories("mods");
                lb_mod.Items.Clear();

                for (int i = 0; i < tmp.Length; i++)
                {
                    tmp[i] = tmp[i].Substring(tmp[i].IndexOf("\\") + 1);
                    if (tmp[i].Length <= 8) lb_mod.Items.Add(tmp[i]);
                }
                tmp = null;
                if (lb_mod.Items.Count == 0)
                    MessageBox.Show("No mods installed.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Mods directory missing.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }

        private void lb_mod_SelectedIndexChanged(object sender, EventArgs e)
        {
            bt_start.Enabled = lb_mod.SelectedIndex != -1;
            bt_info.Enabled = bt_start.Enabled;
        }

        private void bt_start_Click(object sender, EventArgs e)
        {
            folder = "MODS\\" + lb_mod.Items[lb_mod.SelectedIndex] + "\\";
            Process.Start("gta_sa.exe");

        retry:
            if (!Memory.IsProcessOpen("gta_sa"))
            {
                System.Threading.Thread.Sleep(500);
                goto retry;
            }

            mem = new Memory("gta_sa", 0x001F0FFF);
            mem.SetProtection(0x86B000, 0x1000, Memory.Protection.PAGE_READWRITE);

            //Zero out block
            byte[] Buffer = BitConverter.GetBytes(0);
            mem.WriteByte(0x86B34D, Buffer, 0x600);

            //ASCII Dump
            Buffer = encoder.GetBytes(folder);
            mem.WriteString(0x86B34D, Buffer, Buffer.Length);
            tmpaddr = (uint)(0x86B34D + Buffer.Length + 1);
            mainaddr1 = Convert.ToUInt32((0x86B34D).ToString("X") + "68", 16);
            mainaddr2 = Convert.ToUInt32(tmpaddr.ToString("X") + "68", 16);
            Buffer = encoder.GetBytes("MODS\\" + lb_mod.Items[lb_mod.SelectedIndex]);
            mem.WriteString(tmpaddr, Buffer, Buffer.Length);
            tmpaddr += (uint)(Buffer.Length + 1);

            for (int i = 0; i < files.Length; i++)
            {
                if (!File.Exists(folder + files[i])) continue;

                if (prop[i, 0])
                {
                    Buffer = encoder.GetBytes(folder + files[i]);
                    incaddr = (uint)(Buffer.Length + 1);
                    mem.WriteByte(tmpaddr, Buffer, Buffer.Length);
                    Buffer = BitConverter.GetBytes(Convert.ToUInt32(tmpaddr.ToString("X") + "68", 16));
                    mem.WriteByte(pointers[i, 0], Buffer, Buffer.Length);
                    if (prop[i, 1])
                        mem.WriteByte(pointers[i, 1], Buffer, Buffer.Length);
                    tmpaddr += incaddr;
                }
                else
                {
                    if (prop[i, 1])
                    {
                        Buffer = BitConverter.GetBytes(mainaddr1);
                        mem.WriteByte(pointers[i, 0], Buffer, Buffer.Length);
                        mem.WriteByte(pointers[i, 1], Buffer, Buffer.Length);
                    }
                    else
                    {
                        Buffer = BitConverter.GetBytes(mainaddr2);
                        mem.WriteByte(pointers[i, 0], Buffer, Buffer.Length);
                    }
                }
            }
            Application.Exit();
        }

        private void bt_info_Click(object sender, EventArgs e)
        {
            folder = "MODS\\" + lb_mod.Items[lb_mod.SelectedIndex] + "\\";
            if (File.Exists(folder + modinfo_file))
            {
                sr = new StreamReader(folder + modinfo_file);
                MessageBox.Show(sr.ReadToEnd(), "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                sr.Close();
            }
            else MessageBox.Show("This mod doesn't have an information file.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
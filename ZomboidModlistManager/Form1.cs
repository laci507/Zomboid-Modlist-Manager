using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using HtmlAgilityPack;

namespace ZomboidModlistManager
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        //Define what a mod is
        struct Mod
        {
            public Mod(long ID, List<string> Maps, string Name = "", string URL = "", string Description = "")
            {
                this.ID = ID;
                this.Maps = Maps;
                this.Name = Name;
                this.URL = URL;
                this.Description = Description;
            }

            public long ID;
            public List<string> Maps;
            public string Name;
            public string URL;
            public string Description;
        }

        //Regex patterns for getting ID from url
        List<string> steamUrlPatterns = new List<string>
        {
            @"^https?://steamcommunity\.com/sharedfiles/filedetails/\?id=(\d+)$",
            @"^https?://steamcommunity\.com/workshop/filedetails/\?id=(\d+)$",
            @"^https?://steamcommunity\.com/sharedfiles/filedetails/\?id=(\d+)(?:&searchtext=(.*))?$",
            @"^https?://steamcommunity\.com/workshop/filedetails/\?id=(\d+)(?:&searchtext=(.*))?$",
            @"^\d+$"
        };
        List<Mod> mods = new List<Mod>();

        //Texts to match Steam description.
        //This wouldn't be needed, if mod devs would not change the description of uploaded mods. (The game auto-generates it after uploading.)
        string[] modsIDTexts = { "mod id: ", "mod id:", "mods id: ", "mods id:", "mod id ", "mod id", "mods id ", "mods id" };
        string[] mapTexts = { "map folder: ", "map: ", "maps:", "map folder ", "map folder" };

        ///TODO
        ///check mod dependencies
        ///load and write to original config file
        ///
        ///NOT doing: Add multiple mod IDs - this could cause issues.
        ///           Some alternate mod-ids remove mod functionality - this should be user selectable.
        ///*/
        ///

        //Parse Steam urls and normal numeric input - return 0 if not matched.
        long ParseInputUrl(string input)
        {
            foreach (string pattern in steamUrlPatterns)
            {
                Regex regex = new Regex(pattern);
                Match match = regex.Match(textBox1.Text);
                if (match.Success)
                {
                    if (match.Groups[1].Value == "")
                    {
                        return long.Parse(match.Value);
                    }
                    else
                    {
                        return long.Parse(match.Groups[1].Value);
                    }
                }
            }
            return 0;
        }

        //Get all instances of a substring in a string
        List<int> AllIndexesOf(string str, string value)
        {
            if (String.IsNullOrEmpty(value))
                throw new ArgumentException("the string to find should not be empty", "value");
            List<int> indexes = new List<int>();
            for (int index = 0; ; index += value.Length)
            {
                index = str.IndexOf(value, index);
                if (index == -1)
                    return indexes;
                indexes.Add(index);
            }
        }

        //Add mod to list
        void AddModToView(Mod mod)
        {
            //Add item to be displayed
            string Maps = "";
            foreach (string map in mod.Maps)
            {
                Maps += map + "\n ";
            }

            string[] itemToAdd =
            {
                mod.ID.ToString(),
                mod.Name,
                Maps,
                mod.Description
            };
            listView1.Items.Add(new ListViewItem(itemToAdd));
            textBox1.Text = "";
        }

        //Get mod info from Steam
        Mod GetMod(long ID)
        {
            string modName = "";
            string description = "";
            List<string> Maps = new List<string>();
            try
            {
                using (WebClient wc = new WebClient())
                {
                    wc.Proxy = null;

                    string steamPageHTML = wc.DownloadString(@"https://steamcommunity.com/sharedfiles/filedetails/?id=" + ID);

                    var page = new HtmlAgilityPack.HtmlDocument();
                    page.LoadHtml(steamPageHTML);

                    description = page.DocumentNode.SelectNodes("//div[@id='highlightContent']")[0].InnerHtml;

                    if (description != null)
                    {
                        string rawDescription = description;
                        description = page.DocumentNode.SelectNodes("//div[@id='highlightContent']")[0].InnerText;
                        //label_debug.Text = description;

                        //Search description for ModID
                        foreach (string modIDText in modsIDTexts)
                        {
                            int modIDTextIndex = rawDescription.ToLower().IndexOf(modIDText.ToLower());

                            if (modIDTextIndex == -1)
                                continue;

                            //Try
                            modName = rawDescription.Substring(modIDTextIndex + modIDText.Length).Split('<')[0];
                            if (modName != "")
                                break;

                            //Try harder
                            modName = rawDescription.Substring(modIDTextIndex + modIDText.Length).Split('>')[1].Split('<')[0];
                            if (modName != "")
                                break;
                        }

                        //Search description for Map Folder
                        foreach (string mapText in mapTexts)
                        {
                            string map = "";
                            List<int> mapTextIndexes = AllIndexesOf(rawDescription.ToLower(), mapText);

                            /*
                            //Workaround for the stupid SaveOurStations table in the description. (Why would you do a TABLE THERE!?)
                            if (ID == 2398274461)
                            {
                                if (description.ToLower().Contains(mapText))
                                {
                                    int index = mapTextIndexes[0];
                                    Maps.Add(rawDescription.Substring(index + 43, 32));
                                }
                                continue;
                            }*/
                            //Nah, fuck this. This does not work.

                            foreach (int mapTextIndex in mapTextIndexes)
                            {
                                map = rawDescription.Substring(mapTextIndex + mapText.Length).Split('<')[0].Trim();
                                if (map != "")
                                {
                                    if (!Maps.Contains(map) && map != "")
                                    {
                                        Maps.Add(map);
                                    }
                                }
                            }
                        }

                        modName = modName.Trim();
                        label_debug.Text = "Item added:\n" + modName;
                        return new Mod(ID, Maps, modName, "https://steamcommunity.com/sharedfiles/filedetails/?id=" + ID, description);
                    }
                    else
                    {
                        label_debug.Text = "Can't find Steam Workshop Item Description.\nMod may have been deleted!";
                        return new Mod(0, new List<string>());
                    }
                }
            }
            catch (Exception)
            {
                label_debug.Text = "Can't parse data from Steam description.";
                return new Mod(ID, new List<string>(), modName, "https://steamcommunity.com/sharedfiles/filedetails/?id=" + ID, description);
            }
        }

        //Add mod button
        private void button1_Click(object sender, EventArgs e)
        {
            long ID = ParseInputUrl(textBox1.Text);

            if (ID == 0)
            {
                label_debug.Text = "Can't match workshop item ID.";
                return;
            }

            if (mods.Where(x => x.ID == ID).Any())
            {
                label_debug.Text = "Already in list.";
                return;
            }

            Mod currentMod = GetMod(ID);

            if (currentMod.ID == 0)
                return;

            //Add item to list
            mods.Add(currentMod);

            AddModToView(currentMod);
            listView1.EnsureVisible(listView1.Items.Count - 1);
            listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
        }

        //Remove selected items button
        private void button2_Click(object sender, EventArgs e)
        {
            while (listView1.CheckedItems.Count > 0)
            {
                label_debug.Text = "Item removed:\n" + mods[listView1.CheckedItems[0].Index];
                mods.RemoveAt(listView1.CheckedItems[0].Index);
            }
            listView1.Items.Clear();
            foreach (Mod mod in mods)
            {
                AddModToView(mod);
            }
            listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
        }

        //Write to file button
        private void button5_Click(object sender, EventArgs e)
        {
            if (!mods.Any())
            {
                label_debug.Text = "No mods present.\nNothing is written to file.";
                return;
            }

            string modNameOutput = "";
            string modIDOutput = "";
            string mapOutput = "";

            foreach (Mod mod in mods)
            {
                modNameOutput += mod.Name + ";";
                modIDOutput += mod.ID + ";";
                foreach (string map in mod.Maps)
                {
                    mapOutput += map + ";";
                }
            }

            StreamWriter sr = new StreamWriter("mods.txt", false, Encoding.Default);

            sr.Write("Mods=");
            sr.WriteLine(modNameOutput.Substring(0, modNameOutput.Length - 1) + "\n");
            sr.Write("WorkshopItems=");
            sr.WriteLine(modIDOutput.Substring(0, modIDOutput.Length - 1));
            if (mapOutput != "")
            {
                sr.Write("\nMap=");
                sr.WriteLine(mapOutput.Substring(0, mapOutput.Length - 1));
            }
            sr.Close();

            label_debug.Text = "Written to file.";
        }

        //Move mod up button
        private void button3_Click(object sender, EventArgs e)
        {
            //Error handling
            if (mods.Count < 2)
            {
                label_debug.Text = "No mods to rearrange.";
                return;
            }
            if (listView1.CheckedItems.Count != 1)
            {
                label_debug.Text = "Only select one item to move.";
                return;
            }

            //Swap
            int index = listView1.CheckedItems[0].Index;
            if (index == 0)
            {
                label_debug.Text = "Upper boundary reached.";
                return;
            }
            Mod temp = mods[index - 1];
            mods[index - 1] = mods[index];
            mods[index] = temp;

            //Update view
            listView1.Items.Clear();
            foreach (Mod mod in mods)
            {
                AddModToView(mod);
            }
            listView1.Items[index - 1].Checked = true;
        }

        //Move mod down button
        private void button6_Click(object sender, EventArgs e)
        {
            //Error handling
            if (mods.Count < 2)
            {
                label_debug.Text = "No mods to rearrange.";
                return;
            }
            if (listView1.CheckedItems.Count != 1)
            {
                label_debug.Text = "Only select one item to move.";
                return;
            }

            //Swap
            int index = listView1.CheckedItems[0].Index;
            if (index == listView1.Items.Count - 1)
            {
                label_debug.Text = "Lower boundary reached.";
                return;
            }
            Mod temp = mods[index + 1];
            mods[index + 1] = mods[index];
            mods[index] = temp;

            //Update view
            listView1.Items.Clear();
            foreach (Mod mod in mods)
            {
                AddModToView(mod);
            }
            listView1.Items[index + 1].Checked = true;
        }

        //Read from file (custom mods.txt file)
        private void button4_Click(object sender, EventArgs e)
        {
            //Read data
            string[] raw = File.ReadAllLines("mods.txt", Encoding.Default);
            string modsString = "";
            bool foundLine = false;

            label_debug.Text = "Reading...";
            Application.DoEvents();
            foreach (string line in raw)
            {
                if (line.Length >= 14)
                {
                    if (line.Substring(0, 14) == "WorkshopItems=")
                    {
                        modsString = line.Substring(14);
                        foundLine = true;
                        break;
                    }
                }
            }

            //Error handling
            if (!foundLine)
            {
                label_debug.Text = "'WorkshopItems=' line not found.";
                return;
            }
            if (modsString == "")
            {
                label_debug.Text = "No data in 'mods.txt'";
                return;
            }

            //Remove old mods, add new ones and display them
            mods.Clear();
            listView1.Items.Clear();
            string[] modsArray = modsString.Split(';');

            foreach (string item in modsArray)
            {
                Mod mod = GetMod(long.Parse(item));
                mods.Add(mod);

                AddModToView(mod);
                Application.DoEvents();
                listView1.EnsureVisible(listView1.Items.Count - 1);
                listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            }
            label_debug.Text = "Done.\nAdded " + mods.Count + " items.";
        }

        //Open item in browser on double click
        private void listView1_ItemActivate(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(mods[listView1.SelectedItems[0].Index].URL);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Windows.Forms;
using Fizzler.Systems.HtmlAgilityPack;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading;

namespace MusicDownloader
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        public void search(string objectOfSearch)
        {
            richTextBox1.Invoke(new Action<string>((s) => richTextBox1.Text = s), "");
            int quanOfPages = 1;
            for (int page = 1; page <= quanOfPages; page++)
            {
                string link = getUrl(objectOfSearch, page);
                string htmlCode = getHtml(link);
                string textInRTb = "";
                List<int> noLinksSongs = new List<int>();
                string[] song = getSongsOnPage(link);
                for (int i = 0; i < song.Length; i++)
                {
                    textInRTb += song[i] + "\n";
                }
                List<string> HREFS = getHREFtoPages(htmlCode);
                HREFS = sortHrefs(HREFS);
                while (HREFS.Count > song.Length)
                    HREFS.RemoveAt(HREFS.Count - 1);
                for (int i = 0; i < song.Length; i++)
                {
                    richTextBox1.Text += song[i] + " " + HREFS[i] + "\n";
                }
                if (song.Length == 0) richTextBox1.Text = "Песни с таким названием не найдено.";
                quanOfPages = getQuanOfPages(link);
            }
        }

        public string getUrl(string objectOfSearch, int page)
        {
            string link = "";
            link = "http://zaycev.net/search.html?page=" + page + "&query_search=";
            link += objectOfSearch;
            return link;
        }
        public List<string> getHREFtoPages(string htmlCode)
        {
            string[] htmlString = htmlCode.Split('\n');
            List<string> HREFS = new List<string>();
            string buf = "";
            for (int j = 0; j < htmlString.Length; j++)
            {
                for (int i = 0; i < htmlString[j].Length - 12; i++)
                {
                    if (htmlString[j].Substring(i, 12) == "href=\"/pages")
                    {
                        buf = "";
                        i += 6;
                        while (htmlString[j].Substring(i, 1) != @"""")
                        {
                            buf += htmlString[j].Substring(i, 1);
                            i++;
                        }
                        buf = "http://zaycev.net" + buf;
                        HREFS.Add(buf);
                    }
                }
            }
            return HREFS;
        }
        public List<string> sortHrefs(List<string> HREFS)
        {
            List<string> sortingHREFS = new List<string>();
            for (int i = 0; i < HREFS.Count; i++)
            {
                for (int j = 0; j < HREFS.Count; j++)
                {
                    if ((HREFS[i] == HREFS[j]) && (i != j))
                        HREFS[j] = null;
                }
                if (HREFS[i] != null)
                    sortingHREFS.Add(HREFS[i]);
            }
            return sortingHREFS;
        }
        public string[] editTime(string[] song)
        {
            for (int i = 0; i < song.Length; i++)
            {
                for (int j = 0; j < song[i].Length; j++)
                {
                    if (song[i][j] == ':')
                    {
                        string songLength = song[i].Remove(0, j - 2);
                        song[i] = song[i].Remove(j - 2);
                        song[i] += "(" + songLength + ")";
                        j += 2;
                    }
                }
            }
            return song;
        }
        public string[] getSongsOnPage(string link)
        {
            string htmlCode = getHtml(link);
            string text = "";
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(htmlCode);
            foreach (var item in doc.DocumentNode.QuerySelectorAll("div.search-page__tracks"))
            {
                text += item.InnerText;
            }
            string[] lines = text.Split('\n');
            string[] song = new string[lines.Length - 1];
            for (int i = 0; i < song.Length; i++)
                song[i] = lines[i + 1];
            song = editTime(song);
            return song;
        }
        public int getQuanOfPages(string link)
        {
            string htmlCode = getHtml(link);
            string text = "";
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(htmlCode);
            foreach (var item in doc.DocumentNode.QuerySelectorAll("div.search-pager-container"))
            {
                text += item.InnerText;
            }
            if (text != "") //если не одна страница
            {
                if (text.Length > 9)
                    text = text.Remove(text.Length - 9); //если это не последняя страница
                int lastPage = Convert.ToInt32(text.Remove(0, text.Length - 1));
                return lastPage;
            }
            else return 1;
        }
        public string getHtml(string link)
        {
            string htmlCode;
            WebClient client = new WebClient();
            client.DownloadFile(link, "test.html");
            htmlCode = File.ReadAllText("test.html");
            return htmlCode;
        }
        public string getHREFtoSave(string htmlCode)
        {
            string[] htmlString = htmlCode.Split('\n');
            string buf = "";
            for (int j = 0; j < htmlString.Length; j++)
            {
                for (int i = 0; i < htmlString[j].Length - 18; i++)
                {
                    if (htmlString[j].Substring(i, 18) == "href=\"http://cdndl")
                    {
                        buf = "";
                        i += 6;
                        while (htmlString[j].Substring(i, 1) != @"""")
                        {
                            buf += htmlString[j].Substring(i, 1);
                            i++;
                        }
                    }
                }
            }
            return buf;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (tabControl2.SelectedIndex == 0)
            {
                tabControl2.SelectedIndex = 1;
                printStat();
                backButton.Visible = true;
            }
            else
            {
                tabControl2.SelectedIndex = 0;
                backButton.Visible = false;
            }
        }
        public void printStat()
        {
            richTextBox2.Text = "";
            string connectionString = ConfigurationManager.ConnectionStrings["MusicDownloader.Properties.Settings.userdbConnectionString"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string sqlExpression = "SELECT * FROM Songs";
                SqlCommand command = new SqlCommand(sqlExpression, connection);
                SqlDataReader reader = command.ExecuteReader();
                List<string> name = new List<string>();
                while (reader.Read())
                {
                    name.Add(reader["name"].ToString());
                }
                string[] masOfPerf = name.ToArray();
                for (int i = 0; i < masOfPerf.Length; i++)
                {
                    if (masOfPerf[i] != "")
                    {
                        richTextBox2.Text += masOfPerf[i];
                        int qualOfRep = 1;
                        for (int j = 0; j < masOfPerf.Length; j++)
                        {
                            if ((masOfPerf[j] != "") && (masOfPerf[i] == masOfPerf[j]) && (i != j))
                            {
                                qualOfRep++;
                                masOfPerf[j] = "";
                            }
                        }
                        richTextBox2.Text += String.Format("[{0}] \n", qualOfRep.ToString());
                    }
                }
            }
        }
        private void findBtn_Click_1(object sender, EventArgs e)
        {
            search(requestTB.Text);
        }

        private void requestTB_KeyPress_1(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                findBtn.PerformClick();
            }
        }

        private void richTextBox1_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["MusicDownloader.Properties.Settings.userdbConnectionString"].ConnectionString;
            string htmlCode = getHtml(e.LinkText);
            string link = getHREFtoSave(htmlCode);
            int lenthOfName = 0, beginOfName = 0;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                int endOfName = richTextBox1.Text.IndexOf(e.LinkText) - 3; // begin of link - 3
                for (int i = 0; i < 1000; i++) //считываем название по одному символу с конца
                {

                    if (endOfName - i == 0) // если стоит первым и дошли до начала строки
                    {
                        lenthOfName = i;
                        beginOfName = 0;
                        break;
                    }
                    else if (richTextBox1.Text.Substring(endOfName - i, 1) == "\n")
                    {
                        lenthOfName = i - 1;
                        beginOfName = endOfName - i + 1;
                        break;
                    }
                }
                string name = richTextBox1.Text.Substring(beginOfName, lenthOfName - 7);
                string[] performAndName = name.Split(Convert.ToChar("–"));
                SqlCommand cmd = new SqlCommand();
                cmd.CommandText = "INSERT INTO Songs (Name, DateTime) VALUES (@name, @datetime)";
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.Parameters.AddWithValue("@name", performAndName[0]);
                cmd.Parameters.AddWithValue("@datetime", System.DateTimeOffset.Now);
                cmd.Connection = connection;
                cmd.ExecuteNonQuery();
            }
            try
            {
                System.Diagnostics.Process.Start(link);
            }
            catch (Exception InvalidOperationException)
            {
                MessageBox.Show(
                    "Ссылка на скачивание отсутствует",
                    "Ошибка!",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information,
                    MessageBoxDefaultButton.Button1
                    );
            }
        }
        private void backButton_Click(object sender, EventArgs e)
        {
            tabControl2.SelectedIndex = 0;
            backButton.Visible = false;
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LOL_VisualArtChanger
{
    public partial class Form1 : Form
    {
        const string kTargetAppName = "League of Legends";
        //const string kTargetAppName = "Bandicam";
        const string kKeyName = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall";
        const string kTargetValueName = "InstallLocation";
        const string kAssetDir = @"RADS\projects\lol_air_client\releases\0.0.1.157\deploy\assets\images\champions";
        const string kLolJapaneseWikiURL = @"http://loljp-wiki.tk/wiki/";
        const string kLolEngiishWikiURL = @"http://leagueoflegends.wikia.com/wiki/League_of_Legends_Wiki";

        string mAssetFullPath = string.Empty;
        List<string> mChampionNames;    //< チャンピオン名。「Aatrox」など。
        List<string> mChampionImageFileNames;   //< 拡張子付き。「Aatrox_0.jpg」など。
        List<string> mAssetChampionImageFileFullPaths;   //< パス
        Dictionary<string, int> mChampionsSkinCount;   //< チャンピオンごとのスキン数


        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //... クライアント領域を変更する。
            // Editor上ではウィンドウサイズしか変更できないらしい。
            const int kWidth = 800;
            const int kHeight = 600;
            this.ClientSize = new Size(kWidth, kHeight);

            // PictureBoxのD＆Dを有効にする。
            this.pictureBox2.AllowDrop = true;
            this.pictureBox4.AllowDrop = true;
            this.pictureBox6.AllowDrop = true;

            // ProgressBarの初期設定を行う。
            this.progressBar1.Minimum = 0;
            this.progressBar1.Maximum = 100;
            this.labelProgressValue.Text = "0%";


            // 情報をストアする。
            StoreData();

            // TreeViewを構築する。
            BuildTreeViewChampions();
        }

        /// <summary>
        /// 情報をストアする。
        /// </summary>
        private void StoreData()
        {
            // 指定の名前と一致したなら
            string appOfficialName = string.Empty;
            if (IsApplicationInstalled(kTargetAppName, out appOfficialName))
            {
                // 値名で走査する。
                Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(kKeyName + "\\" + appOfficialName);
                foreach (string strValueName in regKey.GetValueNames())
                {
                    Console.WriteLine(strValueName);

                    // 指定の名前と一致したなら
                    string strValue = regKey.GetValue(strValueName).ToString();
                    if (strValueName == kTargetValueName)
                    {
                        Console.WriteLine("\t" + strValue);

                        // レジストリに登録された値のパスから、アセットまでのパスを構築する。
                        // UNDONE:途中でバージョン番号のディレクトリを挟んでいる。アップデートや複数ディレクトリがある場合に備えて、検知したい。
                        strValue = strValue.Replace("\"", string.Empty);// ダブルクォーテーションを除外。
                        mAssetFullPath = strValue + kAssetDir;

                        // ファイル名のリストを取得する。
                        // UNDONE:実は綺麗にChampionの名前があるわけではない…除外できるなら除外したい。
                        string[] entryNames = System.IO.Directory.GetFileSystemEntries(mAssetFullPath);
                        mAssetChampionImageFileFullPaths = new List<string>(entryNames);

                        break;
                    }
                }
            }
            else
            {
                return;
            }

            // ファイル名だけのリストを作成する。
            mChampionImageFileNames = new List<string>();
            for (int i = 0; i < mAssetChampionImageFileFullPaths.Count; ++i)
            {
                string systemFileName = System.IO.Path.GetFileName(mAssetChampionImageFileFullPaths[i]);
                mChampionImageFileNames.Add(systemFileName);
            }

            // チャンピオン名を抽出する
            mChampionNames = new List<string>();
            foreach (string fullPath in mAssetChampionImageFileFullPaths)
            {
                // ナンバリングと拡張子を省く。
                string fileName = System.IO.Path.GetFileNameWithoutExtension(fullPath);
                string championName = string.Empty;
                int index = fileName.IndexOf("_");
                if (0 <= index)
                {
                    championName = fileName.Substring(0, index);
                }

                // まだ追加されていないなら追加する。
                if (!mChampionNames.Contains(championName))
                {
                    mChampionNames.Add(championName);
                }
            }

            // チャンピオン毎のスキン数を数える。
            mChampionsSkinCount = new Dictionary<string, int>();
            foreach (string championName in mChampionNames)
            {
                int count = 0;
                List<string> fileNamesByChampion = mChampionImageFileNames.FindAll(x => x.Split('.').First().Contains(championName));
                foreach (string fileName in fileNamesByChampion)
                {
                    string sub = fileName.Split('_').Last();
                    sub = sub.Split('.').First();

                    // 数値に変換できるなら処理する。
                    int number = 0;
                    if (int.TryParse(sub, out number))
                    {
                        count = Math.Max(count, number + 1);
                    }
                }

                mChampionsSkinCount.Add(championName, count);
            }
        }

        /// <summary>
        /// 指定のアプリケーションがインストールされているかどうかを取得する。
        /// </summary>
        /// <param name="appName">対象のアプリケーション名</param>
        /// <param name="appOfficialName">あぷりけーしょんのせいしきｍ</param>
        /// <returns>インストールされているかどうか</returns>
        private bool IsApplicationInstalled(string appName, out string appOfficialName)
        {
            appOfficialName = string.Empty;

            // "HKEY_LOCAL_MACHINE/"は省略する。
            Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(kKeyName, false);

            // キー名で走査する。
            string[] strKeyNames = regKey.GetSubKeyNames();
            foreach (string strKeyName in strKeyNames)
            {
                Console.WriteLine(strKeyName);

                // 指定の名前と一致したなら
                if (strKeyName.StartsWith(appName))
                {
                    appOfficialName = strKeyName;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// チャンピオンのTreeViewを構築する
        /// </summary>
        private void BuildTreeViewChampions()
        {
            this.treeView1.Nodes.Clear();

            // ノードにはカード名だけ追加し、SplashとSquareはいらない。
            // 一度に表示されるため。

            foreach(KeyValuePair<string, int> dict in mChampionsSkinCount)
            {
                TreeNode topNode = new TreeNode(dict.Key);

                // 子にスキン用のノードを追加する。
                // HACK正式なSkin名で登録すると分かりやすいが、ファイル名からは判断ができない。
                for (int i = 0; i < dict.Value; ++i)
                {
                    string cardName = dict.Key + "_" + i;
                    TreeNode childNode = new TreeNode(cardName);
                    topNode.Nodes.Add(childNode);
                }

                this.treeView1.Nodes.Add(topNode);
            }

            this.treeView1.Refresh();
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            // 選択中のチャンピオン名を取得する。
            string selectedChampionName = string.Empty;
            string selectedChampionSkinName = string.Empty;
            if (e.Node.Level == 0)
            {
                selectedChampionName = e.Node.Text;
            }
            else
            {
                selectedChampionName = e.Node.Parent.Text;
                selectedChampionSkinName = e.Node.Text;
            }

            // チャンピオン名が見つからない場合、return
            if (selectedChampionName == string.Empty)
            {
                return;
            }

            // 選択したチャンピオンのSquareを読み込む。
            // Squareは1つしかないので先に処理する。
            if (0 <= e.Node.Level)
            {
                const string kSquareStr = "_Square_0";

                // Square変更前
                string championSquareFilePath = mAssetChampionImageFileFullPaths.Find(x => x.Contains(selectedChampionName + kSquareStr));
                if (this.pictureBox1.ImageLocation != championSquareFilePath)
                {
                    this.pictureBox1.ImageLocation = championSquareFilePath;
                }

                // Square変更後。選択した直後は変更前と同じ。
                if (this.pictureBox2.ImageLocation != championSquareFilePath)
                {
                    this.pictureBox2.ImageLocation = championSquareFilePath;
                }

                // その他の画像は破棄しておく。
                this.pictureBox3.ImageLocation = string.Empty;
                this.pictureBox4.ImageLocation = string.Empty;
                this.pictureBox5.ImageLocation = string.Empty;
                this.pictureBox6.ImageLocation = string.Empty;
            }

            // 選択したチャンピオンのCardとSplashを読み込む。
            // 読み込む画像は選択しているスキンに対応する。
            if (1 <= e.Node.Level)
            {
                const string kSplashStr = "_Splash";
                string championSplashFileName = selectedChampionSkinName.Insert(selectedChampionSkinName.Length - 2, kSplashStr);

                // Splash変更前
                string championSkinSplashFilePath = mAssetChampionImageFileFullPaths.Find(x => x.Contains(championSplashFileName));
                this.pictureBox3.ImageLocation = championSkinSplashFilePath;

                // Splash変更後。選択した直後は変更前と同じ。
                this.pictureBox4.ImageLocation = championSkinSplashFilePath;


                // Card変更前
                string championSkinCardFilePath = mAssetChampionImageFileFullPaths.Find(x => x.Contains(selectedChampionSkinName));
                this.pictureBox5.ImageLocation = championSkinCardFilePath;

                // Card変更後
                this.pictureBox6.ImageLocation = championSkinCardFilePath;
            }

        }

        private void pictureBox2_DragDrop(object sender, DragEventArgs e)
        {
            PictureBox picBox = sender as PictureBox;

            // 複数ファイルがD&Dされた際、先頭のファイルのみを対象にする。
            // UNDONE:拡張子判定。
            string[] fileNames = e.Data.GetData(DataFormats.FileDrop) as string[];
            string tgtFileName = fileNames.First();

            // 存在チェック
            if (!System.IO.File.Exists(tgtFileName))
            {
                return;
            }

            // MEMO:画像をリサイズして保存する機能を実装したので、違う拡張子が来ても大丈夫になった。
            // 拡張子同一チェック
            //string extPicBox = System.IO.Path.GetExtension(picBox.ImageLocation);
            //string extTgtFileName = System.IO.Path.GetExtension(tgtFileName);
            //if (extPicBox == string.Empty ||
            //    extPicBox != extTgtFileName)
            //{
            //    return;
            //}

            // 画像を変更する。
            picBox.ImageLocation = tgtFileName;

            // 画像が消えるとD&Dの領域が分かり辛いので何もしない。
            //{
            //    picBox.ImageLocation = string.Empty;
            //}
        }

        private void pictureBox2_DragEnter(object sender, DragEventArgs e)
        {
            // ファイルの場合のみ受け付ける
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.All;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            // UNDONE:変更前のファイルを退避する。

            const int progressBarDelta = 33;
            this.progressBar1.Value = 0;
            this.labelProgressValue.Text = this.progressBar1.Value.ToString() + "%";

            // Squareの画像を上書きコピーする。
            if (this.pictureBox1.ImageLocation != this.pictureBox2.ImageLocation)
            {
                System.IO.File.Copy(this.pictureBox2.ImageLocation, this.pictureBox1.ImageLocation, true);

                // 画像をリサイズする。
                ResizePicture(this.pictureBox1.ImageLocation, 120, 120, System.Drawing.Imaging.ImageFormat.Png);

                // 変更前の画像を変更後の画像にする。
                this.pictureBox1.ImageLocation = this.pictureBox2.ImageLocation;
            }

            // 進捗を更新する。
            this.progressBar1.Value += progressBarDelta;
            this.labelProgressValue.Text = this.progressBar1.Value.ToString() + "%";
            this.labelProgressValue.Update();
            System.Threading.Thread.Sleep(1);

            // Splashの画像を上書きコピーする。
            if (this.pictureBox3.ImageLocation != this.pictureBox4.ImageLocation)
            {
                System.IO.File.Copy(this.pictureBox4.ImageLocation, this.pictureBox3.ImageLocation, true);

                // 画像をリサイズする。
                ResizePicture(this.pictureBox3.ImageLocation, 1215, 717, System.Drawing.Imaging.ImageFormat.Jpeg);

                // 変更前の画像を変更後の画像にする。
                this.pictureBox3.ImageLocation = this.pictureBox4.ImageLocation;
            }

            // 進捗を更新する。
            this.progressBar1.Value += progressBarDelta;
            this.labelProgressValue.Text = this.progressBar1.Value.ToString() + "%";
            this.labelProgressValue.Update();
            System.Threading.Thread.Sleep(1);

            // Cardの画像を上書きコピーする。
            if (this.pictureBox5.ImageLocation != this.pictureBox6.ImageLocation)
            {
                System.IO.File.Copy(this.pictureBox6.ImageLocation, this.pictureBox5.ImageLocation, true);

                // 画像をリサイズする。
                ResizePicture(this.pictureBox5.ImageLocation, 308, 560, System.Drawing.Imaging.ImageFormat.Jpeg);

                // 変更前の画像を変更後の画像にする。
                this.pictureBox5.ImageLocation = this.pictureBox6.ImageLocation;
            }

            // 最後の進捗を更新する。
            this.progressBar1.Value = this.progressBar1.Maximum;
            this.labelProgressValue.Text = this.progressBar1.Value.ToString() + "%";
        }

        private void アセットフォルダを開くToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(mAssetFullPath);
        }

        private void LoL_日本語_WikiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(kLolJapaneseWikiURL);
        }

        private void LoL_英語_WikiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(kLolEngiishWikiURL);
        }

        /// <summary>
        /// 画像をリサイズする。
        /// </summary>
        /// <param name="imageLocation">リサイズする画像のパス</param>
        /// <param name="width">リサイズ後の横幅</param>
        /// <param name="height">リサイズ後の縦幅</param>
        /// <param name="imageFormat">画像フォーマット</param>
        private void ResizePicture(string imageLocation, int width, int height, System.Drawing.Imaging.ImageFormat imageFormat)
        {
            System.Drawing.Bitmap bmpSrc = new System.Drawing.Bitmap(imageLocation);
            System.Drawing.Bitmap bmpSrcResize = new System.Drawing.Bitmap(bmpSrc, width, height);

            // 元画像を削除してから保存し直す。
            bmpSrc.Dispose();
            System.IO.File.Delete(imageLocation);

            bmpSrcResize.Save(imageLocation, imageFormat);
        }
    }
}

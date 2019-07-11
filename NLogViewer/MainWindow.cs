using NLogViewer.Properties;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Windows.Forms;

namespace NLogViewer
{
    public partial class MainWindow : Form
    {
        private OpenFileDialog openFileDialog1;
        private List<string[]> fullList;
        private List<string> guids;
        private char separator = '|';

        public MainWindow()
        {
            InitializeComponent();
            SetupDataGridView();
            FillTypeCombo();

            this.separator = (char)Settings.Default["Separator"];
        }

        private void FillTypeCombo()
        {
            combo_type.DataSource = new string[] { string.Empty, "INFO", "WARN", "DEBUG", "ERROR", "FATAL" };
        }

        private void SetupDataGridView()
        {
            this.dataGridView1.ColumnCount = 7;

            this.dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = Color.Navy;
            this.dataGridView1.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            this.dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font(this.dataGridView1.Font, FontStyle.Bold);

            this.dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;
            this.dataGridView1.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            this.dataGridView1.CellBorderStyle = DataGridViewCellBorderStyle.Single;
            this.dataGridView1.GridColor = Color.Black;
            this.dataGridView1.RowHeadersVisible = false;

            this.dataGridView1.Columns[0].Name = "Date";
            this.dataGridView1.Columns[0].Width = 120;

            this.dataGridView1.Columns[1].Name = "Corelation ID";
            this.dataGridView1.Columns[1].Width = 250;
            this.dataGridView1.Columns[1].DefaultCellStyle.Font = new Font(new FontFamily("Calibri"), 10, FontStyle.Bold);

            this.dataGridView1.Columns[2].Name = "Type";
            this.dataGridView1.Columns[2].Width = 100;
            this.dataGridView1.Columns[2].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            this.dataGridView1.Columns[3].Name = "Component";
            this.dataGridView1.Columns[3].Width = 350;

            this.dataGridView1.Columns[4].Name = "Message";
            this.dataGridView1.Columns[4].Width = 545;

            this.dataGridView1.Columns[5].Name = "Url";
            this.dataGridView1.Columns[5].Width = 300;

            this.dataGridView1.Columns[6].Name = "Action";
            this.dataGridView1.Columns[6].Width = 250;

            this.dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView1.MultiSelect = false;
            this.dataGridView1.Dock = DockStyle.Fill;

            openFileDialog1 = new OpenFileDialog
            {
                FileName = "Select a log file",
                Title = "Open log file"
            };
        }

        private void OpenFile_Click(object sender, EventArgs e)
        {
            fullList = new List<string[]>();
            guids = new List<string> { string.Empty };

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                dataGridView1.Rows.Clear();

                ReadDataFromFile();

                corelationIds.DataSource = guids;
                FillFormatCombo();
            }
        }

        private void ReadDataFromFile()
        {
            using (Stream fileStream = File.Open(openFileDialog1.FileName, FileMode.Open))
            using (StreamReader reader = new StreamReader(fileStream))
            {
                string line = null;
                do
                {
                    line = reader.ReadLine();

                    if (line == null)
                        break;

                    string[] parts = line.Split(new char[] { separator }, StringSplitOptions.None);
                    var row = CreateRowItem(parts);

                    fullList.Add(row);
                    dataGridView1.Rows.Add(row);

                    if (parts.Length > 1 && Guid.TryParse(parts[1], out Guid newGuid))
                    {
                        if (!guids.Contains(newGuid.ToString()))
                            guids.Add(newGuid.ToString());
                    }

                } while (true);
            }
        }

        private void FillFormatCombo()
        {
            foreach (DataGridViewRow Myrow in dataGridView1.Rows)
            {
                if (Myrow.Cells[2].Value?.ToString() == "INFO")
                {
                    Myrow.Cells[2].Style.BackColor = Color.LightBlue;
                }
                else if (Myrow.Cells[2].Value?.ToString() == "WARN")
                {
                    Myrow.Cells[2].Style.BackColor = Color.LightPink;
                }
                else if (Myrow.Cells[2].Value?.ToString() == "DEBUG")
                {
                    Myrow.Cells[2].Style.BackColor = Color.LightYellow;
                }
                else if (Myrow.Cells[2].Value?.ToString() == "ERROR")
                {
                    Myrow.Cells[2].Style.BackColor = Color.Tomato;
                }
                else if (Myrow.Cells[2].Value?.ToString() == "FATAL")
                {
                    Myrow.Cells[2].Style.BackColor = Color.Red;
                }
                else
                {
                    Myrow.Cells[2].Style.BackColor = Color.LightGray;
                }
            }
        }

        private void button_filter_Click(object sender, EventArgs e)
        {
            var filteredList = fullList;
            dataGridView1.Rows.Clear();
            var hasFilter = false;

            if (!string.IsNullOrEmpty(corelationIds.SelectedValue?.ToString()))
            {
                hasFilter = true;
                filteredList = filteredList.Where(x => x[1] == corelationIds.SelectedValue.ToString()).ToList();
            }

            if (combo_type.SelectedValue.ToString() != string.Empty)
            {
                hasFilter = true;
                filteredList = filteredList.Where(x => x[2] == combo_type.SelectedValue.ToString()).ToList();
            }

            if (textBox1.Text != string.Empty)
            {
                hasFilter = true;
                filteredList = filteredList.Where(x => x[4].Contains(textBox1.Text)).ToList();
            }

            if (!hasFilter)
            {
                filteredList = fullList;
            }

            filteredList?.ForEach(x =>
            {
                dataGridView1.Rows.Add(x);
            });

            FillFormatCombo();
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        private void watchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFile_Click(null, null);

            var watch = new FileSystemWatcher
            {
                Path = openFileDialog1.FileName.Replace($"\\{openFileDialog1.SafeFileName}", ""),
                Filter = openFileDialog1.SafeFileName,
                NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite,
            };
            watch.Changed += new FileSystemEventHandler(OnChanged);
            watch.EnableRaisingEvents = true;
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            DateTime.TryParse(dataGridView1.Rows[dataGridView1.Rows.GetLastRow(DataGridViewElementStates.None) - 1].Cells[0].Value.ToString(), out DateTime lastRowDate);

            using (Stream fileStream = File.Open(openFileDialog1.FileName, FileMode.Open))
            using (StreamReader reader = new StreamReader(fileStream))
            {
                string line = null;
                do
                {
                    line = reader.ReadLine();
                    if (line == null)
                    {
                        break;
                    }

                    string[] parts = line.Split(new char[] { separator }, StringSplitOptions.None);

                    DateTime.TryParse(parts[0], out DateTime rowDate);

                    if (rowDate > lastRowDate)
                    {
                        var row = CreateRowItem(parts);

                        fullList.Add(row);
                        dataGridView1.Invoke(new Action(() => dataGridView1.Rows.Add(row)));
                        if (parts.Length > 1 && Guid.TryParse(parts[1], out Guid newGuid))
                        {
                            if (!guids.Contains(newGuid.ToString()))
                                guids.Add(newGuid.ToString());
                        }
                    }

                } while (true);
            }

            FillFormatCombo();
            corelationIds.DataSource = guids;
        }

        private static string[] CreateRowItem(string[] parts)
        {
            string[] row;
            if (parts.Length == 1)
            {
                row = new string[] { parts[0], string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty };
            }
            else if (parts.Length == 2)
            {
                row = new string[] { parts[0], parts[1], string.Empty, string.Empty, string.Empty, string.Empty, string.Empty };
            }
            else if (parts.Length == 3)
            {
                row = new string[] { parts[0], parts[1], parts[2], string.Empty, string.Empty, string.Empty, string.Empty };
            }
            else if (parts.Length == 4)
            {
                row = new string[] { parts[0], parts[1], parts[2], parts[3], string.Empty, string.Empty, string.Empty };
            }
            else if (parts.Length == 5)
            {
                row = new string[] { parts[0], parts[1], parts[2], parts[3], parts[4], string.Empty, string.Empty };
            }
            else if (parts.Length == 6)
            {
                row = new string[] { parts[0], parts[1], parts[2], parts[3], parts[4], parts[5], string.Empty };
            }
            else if (parts.Length == 7)
            {
                row = new string[] { parts[0], parts[1], parts[2], parts[3], parts[4], parts[5], parts[6] };
            }
            else
            {
                row = new string[] { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty };
            }

            return row;
        }
    }
}

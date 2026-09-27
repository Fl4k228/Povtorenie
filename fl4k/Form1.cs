#nullable disable

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;

namespace fl4k
{
    // ============================================================
    //  МОДЕЛЬ ДАННЫХ
    // ============================================================
    public class Person
    {
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string Gender { get; set; }   // "М" или "Ж"
        public double Height { get; set; }   // в сантиметрах

        public Person() { }

        public Person(string lastName, string firstName, string gender, double height)
        {
            LastName = lastName;
            FirstName = firstName;
            Gender = gender;
            Height = height;
        }

        public override string ToString()
            => $"{LastName} {FirstName} ({Gender}, {Height} см)";
    }

    // Контейнер для сохранения в файл (обёртка над массивом)
    public class PeopleData
    {
        public List<Person> People { get; set; } = new List<Person>();
        public string GeneratedAt { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    // ============================================================
    //  ГЛАВНАЯ ФОРМА
    // ============================================================
    public partial class Form1 : Form
    {
        // ---------- Общие настройки ----------
        private static readonly Font HeaderFont  = new Font("Segoe UI", 14F, FontStyle.Bold);
        private static readonly Font LabelFont   = new Font("Segoe UI", 10F);
        private static readonly Font MonoFont    = new Font("Consolas", 10F);
        private static readonly Font ResultFont  = new Font("Consolas", 11F, FontStyle.Bold);

        // ---------- Часть 1: физика ----------
        private TextBox txtDensity, txtRadius, txtMass;
        private TextBox txtThickness;
        private Button btnCalcThickness;
        private Label lblPhysFormula;

        // ---------- Часть 2: люди ----------
        private DataGridView gridPeople;
        private Button btnGenerate, btnSaveJson, btnLoadJson, btnCalcStats;
        private TextBox txtStats;
        private Label lblDataStatus;

        // Данные в памяти
        private List<Person> _people = new List<Person>();

        // Пути к файлу по умолчанию
        private const string DefaultJsonPath = "people.json";

        // Источники данных для генерации
        private static readonly string[] MaleLastNames = {
            "Иванов", "Петров", "Сидоров", "Кузнецов", "Смирнов",
            "Попов", "Соколов", "Лебедев", "Козлов", "Новиков"
        };
        private static readonly string[] MaleFirstNames = {
            "Александр", "Дмитрий", "Сергей", "Андрей", "Алексей",
            "Максим", "Иван", "Николай", "Павел", "Владимир"
        };
        private static readonly string[] FemaleLastNames = {
            "Иванова", "Петрова", "Сидорова", "Кузнецова", "Смирнова",
            "Попова", "Соколова", "Лебедева", "Козлова", "Новикова"
        };
        private static readonly string[] FemaleFirstNames = {
            "Анна", "Елена", "Мария", "Ольга", "Татьяна",
            "Наталья", "Ирина", "Светлана", "Екатерина", "Юлия"
        };

        public Form1()
        {
            InitializeComponent();

            Text = "Практическая работа: физика + обработка данных";
            ClientSize = new Size(1100, 700);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(900, 600);
            BackColor = Color.FromArgb(245, 246, 248);
            Font = new Font("Segoe UI", 9F);

            BuildUi();
        }

        // ============================================================
        //  ПОСТРОЕНИЕ ИНТЕРФЕЙСА
        // ============================================================
        private void BuildUi()
        {
            // ---------- Заголовок окна ----------
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Color.FromArgb(37, 99, 235)
            };
            var title = new Label
            {
                Text = "Практическая работа",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(24, 10)
            };
            var subtitle = new Label
            {
                Text = "Толщина диска · Обработка данных о людях",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(219, 234, 254),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(26, 38)
            };
            header.Controls.Add(title);
            header.Controls.Add(subtitle);

            // ---------- Вкладки ----------
            var tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F),
                Padding = new Point(18, 8)
            };

            var tabPhysics = new TabPage("Часть 1. Толщина диска");
            var tabPeople  = new TabPage("Часть 2. Данные о людях");
            tabPhysics.BackColor = Color.White;
            tabPeople.BackColor  = Color.White;

            BuildPhysicsTab(tabPhysics);
            BuildPeopleTab(tabPeople);

            tabs.TabPages.Add(tabPhysics);
            tabs.TabPages.Add(tabPeople);

            Controls.Add(tabs);
            Controls.Add(header);
        }

        // ---------- Часть 1 ----------
        private void BuildPhysicsTab(TabPage tab)
        {
            // Формула сверху
            lblPhysFormula = new Label
            {
                Text = "Из материала плотностью ρ изготовлен диск радиусом r.\n" +
                       "Какая должна быть толщина диска, чтобы он имел массу m?\n\n" +
                       "Формула:  m = ρ · π · r² · h   ⇒   h = m / (ρ · π · r²)",
                Font = new Font("Segoe UI", 11F),
                ForeColor = Color.FromArgb(31, 33, 38),
                Location = new Point(30, 20),
                Size = new Size(700, 100),
                AutoSize = false
            };
            tab.Controls.Add(lblPhysFormula);

            // Плотность
            var lblRho = new Label
            {
                Text = "Плотность ρ (кг/м³):",
                Font = LabelFont,
                Location = new Point(30, 140),
                Size = new Size(220, 24)
            };
            txtDensity = new TextBox
            {
                Location = new Point(260, 138),
                Size = new Size(180, 26),
                Font = MonoFont,
                Text = "7800"
            };
            var lblHintRho = new Label
            {
                Text = "(например, сталь ≈ 7800 кг/м³)",
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                Location = new Point(450, 140),
                Size = new Size(300, 22)
            };
            tab.Controls.Add(lblRho);
            tab.Controls.Add(txtDensity);
            tab.Controls.Add(lblHintRho);

            // Радиус
            var lblR = new Label
            {
                Text = "Радиус r (м):",
                Font = LabelFont,
                Location = new Point(30, 180),
                Size = new Size(220, 24)
            };
            txtRadius = new TextBox
            {
                Location = new Point(260, 178),
                Size = new Size(180, 26),
                Font = MonoFont,
                Text = "0,1"
            };
            tab.Controls.Add(lblR);
            tab.Controls.Add(txtRadius);

            // Масса
            var lblM = new Label
            {
                Text = "Масса m (кг):",
                Font = LabelFont,
                Location = new Point(30, 220),
                Size = new Size(220, 24)
            };
            txtMass = new TextBox
            {
                Location = new Point(260, 218),
                Size = new Size(180, 26),
                Font = MonoFont,
                Text = "2,45"
            };
            tab.Controls.Add(lblM);
            tab.Controls.Add(txtMass);

            // Кнопка
            btnCalcThickness = new Button
            {
                Text = "Рассчитать толщину",
                Location = new Point(260, 265),
                Size = new Size(180, 36),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnCalcThickness.FlatAppearance.BorderSize = 0;
            btnCalcThickness.Click += BtnCalcThickness_Click;
            tab.Controls.Add(btnCalcThickness);

            // Результат
            var lblRes = new Label
            {
                Text = "Толщина диска h (м):",
                Font = LabelFont,
                Location = new Point(30, 320),
                Size = new Size(220, 24)
            };
            txtThickness = new TextBox
            {
                Location = new Point(260, 318),
                Size = new Size(180, 26),
                Font = ResultFont,
                ReadOnly = true,
                BackColor = Color.FromArgb(240, 253, 244),
                ForeColor = Color.FromArgb(21, 128, 61)
            };
            tab.Controls.Add(lblRes);
            tab.Controls.Add(txtThickness);

            // Заметка про Math.PI
            var note = new Label
            {
                Text = "Примечание: в расчёте используется Math.PI (число π ≈ 3,14159...).",
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                Location = new Point(30, 360),
                Size = new Size(600, 22)
            };
            tab.Controls.Add(note);
        }

        // ---------- Часть 2 ----------
        private void BuildPeopleTab(TabPage tab)
        {
            // Панель кнопок сверху
            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(250, 250, 252)
            };

            btnGenerate = MakeButton("Сгенерировать", new Point(10, 12), Color.FromArgb(37, 99, 235));
            btnSaveJson = MakeButton("Сохранить JSON", new Point(160, 12), Color.FromArgb(22, 163, 74));
            btnLoadJson = MakeButton("Загрузить JSON", new Point(310, 12), Color.FromArgb(234, 88, 12));
            btnCalcStats = MakeButton("Рассчитать статистику", new Point(460, 12), Color.FromArgb(168, 85, 247));

            btnGenerate.Click += BtnGenerate_Click;
            btnSaveJson.Click += BtnSaveJson_Click;
            btnLoadJson.Click += BtnLoadJson_Click;
            btnCalcStats.Click += BtnCalcStats_Click;

            topPanel.Controls.Add(btnGenerate);
            topPanel.Controls.Add(btnSaveJson);
            topPanel.Controls.Add(btnLoadJson);
            topPanel.Controls.Add(btnCalcStats);

            // Таблица
            gridPeople = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ColumnHeadersHeight = 34,
                EnableHeadersVisualStyles = false
            };
            gridPeople.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(243, 244, 246);
            gridPeople.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(31, 33, 38);
            gridPeople.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            gridPeople.DefaultCellStyle.Font = new Font("Segoe UI", 9.5F);
            gridPeople.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            gridPeople.DefaultCellStyle.SelectionForeColor = Color.FromArgb(31, 33, 38);

            gridPeople.Columns.Add(new DataGridViewTextBoxColumn { Name = "LastName",  HeaderText = "Фамилия" });
            gridPeople.Columns.Add(new DataGridViewTextBoxColumn { Name = "FirstName", HeaderText = "Имя" });
            gridPeople.Columns.Add(new DataGridViewTextBoxColumn { Name = "Gender",    HeaderText = "Пол" });
            gridPeople.Columns.Add(new DataGridViewTextBoxColumn { Name = "Height",    HeaderText = "Рост, см" });

            // Панель статистики снизу
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 220,
                BackColor = Color.FromArgb(250, 250, 252)
            };

            var lblStats = new Label
            {
                Text = "Результаты расчёта:",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Location = new Point(10, 8),
                Size = new Size(300, 22)
            };

            txtStats = new TextBox
            {
                Location = new Point(10, 34),
                Size = new Size(1050, 140),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = MonoFont,
                BackColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            lblDataStatus = new Label
            {
                Text = "Людей в базе: 0",
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                Location = new Point(10, 182),
                Size = new Size(500, 22)
            };

            bottomPanel.Controls.Add(lblStats);
            bottomPanel.Controls.Add(txtStats);
            bottomPanel.Controls.Add(lblDataStatus);

            // Порядок добавления важен: Fill должен быть между Top и Bottom
            tab.Controls.Add(gridPeople);
            tab.Controls.Add(bottomPanel);
            tab.Controls.Add(topPanel);
        }

        private Button MakeButton(string text, Point loc, Color color)
        {
            var b = new Button
            {
                Text = text,
                Location = loc,
                Size = new Size(140, 36),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        // ============================================================
        //  ЧАСТЬ 1. РАСЧЁТ ТОЛЩИНЫ ДИСКА
        // ============================================================
        private void BtnCalcThickness_Click(object sender, EventArgs e)
        {
            try
            {
                double rho    = ParseNumber(txtDensity.Text);
                double r      = ParseNumber(txtRadius.Text);
                double mass   = ParseNumber(txtMass.Text);

                if (rho <= 0)
                    throw new ArgumentException("Плотность должна быть больше нуля.");
                if (r <= 0)
                    throw new ArgumentException("Радиус должен быть больше нуля.");
                if (mass <= 0)
                    throw new ArgumentException("Масса должна быть больше нуля.");

                // h = m / (ρ · π · r²)  — используем Math.PI
                double thickness = mass / (rho * Math.PI * r * r);

                txtThickness.Text = thickness.ToString("G6");

                // Дополнительно — в мм, если значение маленькое и непустое
                if (thickness < 1.0 && thickness > 0)
                {
                    txtThickness.Text += $"  ({thickness * 1000:F2} мм)";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка расчёта: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Парсит число, принимая как точку, так и запятую как разделитель.
        /// </summary>
        private static double ParseNumber(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("Поле не заполнено.");

            string normalized = input.Trim().Replace(',', '.');
            if (!double.TryParse(normalized, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double value))
                throw new ArgumentException($"Некорректное число: \"{input}\".");

            return value;
        }

        // ============================================================
        //  ЧАСТЬ 2. ГЕНЕРАЦИЯ ДАННЫХ
        // ============================================================
        private void BtnGenerate_Click(object sender, EventArgs e)
        {
            var rnd = new Random();
            int count = 20;  // количество людей

            _people = new List<Person>();
            for (int i = 0; i < count; i++)
            {
                bool male = rnd.Next(2) == 0;
                string lastName  = male ? MaleLastNames[rnd.Next(MaleLastNames.Length)]
                                        : FemaleLastNames[rnd.Next(FemaleLastNames.Length)];
                string firstName = male ? MaleFirstNames[rnd.Next(MaleFirstNames.Length)]
                                        : FemaleFirstNames[rnd.Next(FemaleFirstNames.Length)];
                string gender    = male ? "М" : "Ж";

                // Рост: мужчины 165–200, женщины 155–185
                double height = male
                    ? 165 + rnd.NextDouble() * 35
                    : 155 + rnd.NextDouble() * 30;
                height = Math.Round(height, 1);

                _people.Add(new Person(lastName, firstName, gender, height));
            }

            RefreshGrid();
            lblDataStatus.Text = $"Людей в базе: {_people.Count}  (сгенерировано)";
            txtStats.Clear();
        }

        // ============================================================
        //  ЧАСТЬ 2. СОХРАНЕНИЕ В JSON
        // ============================================================
        private void BtnSaveJson_Click(object sender, EventArgs e)
        {
            if (_people.Count == 0)
            {
                MessageBox.Show("Сначала сгенерируйте данные.",
                    "Нечего сохранять", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var dlg = new SaveFileDialog
            {
                Filter = "JSON-файлы (*.json)|*.json|Все файлы (*.*)|*.*",
                FileName = DefaultJsonPath,
                Title = "Сохранить данные в JSON"
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                var data = new PeopleData { People = _people };
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                string json = JsonSerializer.Serialize(data, options);
                File.WriteAllText(dlg.FileName, json, Encoding.UTF8);

                lblDataStatus.Text = $"Людей в базе: {_people.Count}  —  сохранено в {Path.GetFileName(dlg.FileName)}";

                MessageBox.Show($"Данные сохранены в файл:\n{dlg.FileName}",
                    "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ============================================================
        //  ЧАСТЬ 2. ЗАГРУЗКА ИЗ JSON
        // ============================================================
        private void BtnLoadJson_Click(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Filter = "JSON-файлы (*.json)|*.json|Все файлы (*.*)|*.*",
                Title = "Загрузить данные из JSON"
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                string json = File.ReadAllText(dlg.FileName, Encoding.UTF8);
                var data = JsonSerializer.Deserialize<PeopleData>(json);

                if (data == null || data.People == null || data.People.Count == 0)
                {
                    MessageBox.Show("Файл не содержит данных о людях.",
                        "Пустой файл", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _people = data.People;
                RefreshGrid();
                lblDataStatus.Text = $"Людей в базе: {_people.Count}  —  загружено из {Path.GetFileName(dlg.FileName)}";
                txtStats.Clear();
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"Некорректный JSON: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshGrid()
        {
            gridPeople.Rows.Clear();
            foreach (var p in _people)
                gridPeople.Rows.Add(p.LastName, p.FirstName, p.Gender, p.Height.ToString("F1"));
        }

        // ============================================================
        //  ЧАСТЬ 2. СТАТИСТИКА
        // ============================================================
        private void BtnCalcStats_Click(object sender, EventArgs e)
        {
            if (_people.Count == 0)
            {
                MessageBox.Show("Сначала сгенерируйте или загрузите данные.",
                    "Нет данных", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var men   = _people.Where(p => p.Gender == "М").ToList();
            var women = _people.Where(p => p.Gender == "Ж").ToList();

            var sb = new StringBuilder();
            sb.AppendLine("═══════════════════════════════════════════════════════");
            sb.AppendLine("  РЕЗУЛЬТАТЫ ОБРАБОТКИ ДАННЫХ");
            sb.AppendLine("═══════════════════════════════════════════════════════");
            sb.AppendLine($"Всего человек в базе: {_people.Count}");
            sb.AppendLine($"  Мужчин:  {men.Count}");
            sb.AppendLine($"  Женщин:  {women.Count}");
            sb.AppendLine();

            if (men.Count > 0)
            {
                double avgMale = men.Average(p => p.Height);
                sb.AppendLine($"▸ Средний рост мужчин:  {avgMale:F2} см");

                var tallestMan = men.OrderByDescending(p => p.Height).First();
                sb.AppendLine($"▸ Самый высокий мужчина: {tallestMan.LastName} {tallestMan.FirstName} — {tallestMan.Height:F1} см");
            }
            else
            {
                sb.AppendLine("▸ Мужчин в базе нет.");
            }

            sb.AppendLine();

            if (women.Count > 0)
            {
                double avgFemale = women.Average(p => p.Height);
                sb.AppendLine($"▸ Средний рост женщин:  {avgFemale:F2} см");

                var tallestWoman = women.OrderByDescending(p => p.Height).First();
                sb.AppendLine($"▸ Самая высокая женщина: {tallestWoman.LastName} {tallestWoman.FirstName} — {tallestWoman.Height:F1} см");
            }
            else
            {
                sb.AppendLine("▸ Женщин в базе нет.");
            }

            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════════════════════");

            txtStats.Text = sb.ToString();
        }
    }
}
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using ICommand = PluginBase.ICommand;
using Path = System.IO.Path;

namespace SystemInfo
{
    // Класс реализующий code-behind для главного окна приложения
    public partial class MainWindow : Window
    {
        // Метод инициализируюций визуальные компонены
        public MainWindow()
        {
            InitializeComponent();
        }

        // Интерфейсы перечисления для подключаемых команд модулей
        IEnumerable<ICommand>? infoAboutSystemInternetCommands;

        IEnumerable<ICommand>? infoAboutSystemOSCommands;

        IEnumerable<ICommand>? infoAboutSystemHardwareCommands;

        // Статический метод подключения плагина
        // string relativePath - путь к подключаемому плагину
        static Assembly LoadPlugin(string relativePath)
        {
            // Navigate up to the solution root
            string root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(typeof(MainWindow).Assembly.Location)));

            string pluginLocation = Path.GetFullPath(Path.Combine(root, relativePath.Replace('\\', Path.DirectorySeparatorChar)));
            Console.WriteLine($"Loading commands from: {pluginLocation}");
            PluginLoadContext loadContext = new(pluginLocation);
            return loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(pluginLocation)));
        }

        // Статический метод создания команд для подключенного плагина
        // Assembly assembly - блок (библиотека runtime) подключенный к приложению
        static IEnumerable<ICommand> CreateCommands(Assembly assembly)
        {
            int count = 0;

            foreach (Type type in assembly.GetTypes())
            {
                if (typeof(ICommand).IsAssignableFrom(type))
                {
                    if (Activator.CreateInstance(type) is ICommand result)
                    {
                        count++;
                        yield return result;
                    }
                }
            }

            if (count == 0)
            {
                string availableTypes = string.Join(",", assembly.GetTypes().Select(t => t.FullName));
                throw new ApplicationException(
                    $"Can't find any type which implements ICommand in {assembly} from {assembly.Location}.\n" +
                    $"Available types: {availableTypes}");
            }
        }

        // Метод реализующий добавление в ListView выбранного пользователем модуля
        private void AddPathButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "Файлы библиотек (*.dll)|*.dll|Все файлы (*.*)|*.*"
            };

            openFileDialog.ShowDialog();

            modulesPathes.Items.Add(openFileDialog.FileName);
        }

        // Метод реализующий загрузку из ListView выбранного пользователем модуля 
        private void LoadSelectedModule_Click(object sender, RoutedEventArgs e)
        {
            if (modulesPathes.SelectedItem.ToString() != null)
            {
                string pluginPath = modulesPathes.SelectedItem.ToString();

                Assembly pluginAssembly = LoadPlugin(pluginPath);

                try
                {
                    IEnumerable<ICommand> commands = CreateCommands(pluginAssembly);

                    foreach (ICommand command in commands)
                    {
                        if (command != null)
                        {
                            textBox1.FontSize = 14;

                            textBox1.Background = new SolidColorBrush(Colors.LightGreen);

                            textBox1.Text = "Модуль успешно загружен";

                            Application.Current.Resources["SystemControlHighlightListAccentLowBrush"] = new SolidColorBrush(Colors.Green);
                            Application.Current.Resources["SystemControlHighlightListAccentMediumBrush"] = new SolidColorBrush(Colors.Green);

                            getInfoAboutModule.IsEnabled = true;
                        }
                        else
                        {
                            textBox1.Text = "Модуль загружен с ошибками";

                            Application.Current.Resources["SystemControlHighlightListAccentLowBrush"] = new SolidColorBrush(Colors.Red);
                            Application.Current.Resources["SystemControlHighlightListAccentMediumBrush"] = new SolidColorBrush(Colors.Red);

                            break;
                        }
                    }

                    if (commands.First().Name == "SysInfoInternet")
                    {
                        infoAboutSystemInternetCommands = commands;

                        infoAboutInternet.IsEnabled = true;
                    }
                    else if (commands.First().Name == "SysInfoOS")
                    {
                        infoAboutSystemOSCommands = commands;

                        infoAboutSystem.IsEnabled = true;
                    }
                    else if (commands.First().Name == "SysInfoHardware")
                    {
                        infoAboutSystemHardwareCommands = commands;

                        infoAboutHardware.IsEnabled = true;
                    }
                }

                catch (Exception ex)
                {
                    textBox1.FontSize = 14;

                    textBox1.Background = new SolidColorBrush(Colors.Red);

                    textBox1.Text = "Модуль загружен с ошибками : " + ex.Message;

                    if ((bool)isLogging.IsChecked)
                        Logger.WriteLine("Модуль загружен с ошибками : " + ex.Message);
                }

            }
        }

        // Метод реализующий выгрузку из ListView выбранного пользователем модуля
        private void UnloadSelectedModule_Click(object sender, RoutedEventArgs e)
        {
            // Реализация невозможна из-за невозможности выгрузки домена,
            // который более небезопасен
            throw new NotImplementedException();
        }

        // Метод реализующий отображение общих сведений о системе, полученных из плагина
        private void InfoAboutSystem_Click(object sender, RoutedEventArgs e)
        {
            InfoAboutSystem infoAboutSystem = new();

            foreach (string t in infoAboutSystemOSCommands.First().Execute())
            {
                infoAboutSystem.infoAboutSystemText.Text = infoAboutSystem.infoAboutSystemText.Text + "\n" + t;
            }

            infoAboutSystem.ShowDialog();
        }

        // Метод реализующий отображение сведений об IPv4 и IPv6 полученных из плагина
        private void InfoAboutInternet_Click(object sender, RoutedEventArgs e)
        {
            InfoAboutSystem infoAboutSystem = new();

            foreach (string t in infoAboutSystemInternetCommands.First().Execute())
            {
                infoAboutSystem.infoAboutSystemText.Text = infoAboutSystem.infoAboutSystemText.Text + "\n" + t;
            }

            infoAboutSystem.ShowDialog();
        }

        // Метод реализующий отображение общих сведений о компьютерных комплектующих, полученных из плагина
        private void InfoAboutHardware_Click(object sender, RoutedEventArgs e)
        {
            InfoAboutSystem infoAboutSystem = new InfoAboutSystem();

            foreach (string t in infoAboutSystemHardwareCommands.First().Execute())
            {
                infoAboutSystem.infoAboutSystemText.Text = infoAboutSystem.infoAboutSystemText.Text + "\n" + t;
            }

            infoAboutSystem.ShowDialog();
        }

        // Метод реализующий включение кнопки подключения модуля, после выбора любого элемента ListView
        private void ModulesPathes_Selected(object sender, RoutedEventArgs e)
        {
            loadSelectedModule.IsEnabled = true;
        }

        // Метод реализующий определение и подключение модулей в выбранной пользователем? системном пути
        private void LoadModulesFromPathes_Click(object sender, RoutedEventArgs e)
        {
            string[] modulesPatches = Directory.GetFiles("C:\\temp", "*.dll");

            foreach (string patch in modulesPatches)
            {
                modulesPathes.Items.Add(patch);

                if (!string.IsNullOrEmpty(patch))
                {
                    Assembly pluginAssembly = LoadPlugin(patch);

                    try
                    {
                        IEnumerable<ICommand> commands = CreateCommands(pluginAssembly);

                        foreach (ICommand command in commands)
                        {
                            if (command != null)
                            {
                                textBox1.FontSize = 14;

                                textBox1.Background = new SolidColorBrush(Colors.LightGreen);

                                textBox1.Text = "Модуль успешно загружен";

                                Application.Current.Resources["SystemControlHighlightListAccentLowBrush"] = new SolidColorBrush(Colors.Green);
                                Application.Current.Resources["SystemControlHighlightListAccentMediumBrush"] = new SolidColorBrush(Colors.Green);

                                getInfoAboutModule.IsEnabled = true;
                            }
                            else
                            {
                                textBox1.Text = "Модуль загружен с ошибками";

                                Application.Current.Resources["SystemControlHighlightListAccentLowBrush"] = new SolidColorBrush(Colors.Red);
                                Application.Current.Resources["SystemControlHighlightListAccentMediumBrush"] = new SolidColorBrush(Colors.Red);

                                break;
                            }
                        }

                        if (commands.First().Name == "SysInfoInternet")
                        {
                            infoAboutSystemInternetCommands = commands;

                            infoAboutInternet.IsEnabled = true;
                        }
                        else if (commands.First().Name == "SysInfoOS")
                        {
                            infoAboutSystemOSCommands = commands;

                            infoAboutSystem.IsEnabled = true;
                        }
                        else if (commands.First().Name == "SysInfoHardware")
                        {
                            infoAboutSystemHardwareCommands = commands;

                            infoAboutHardware.IsEnabled = true;
                        }
                    }

                    catch (Exception ex)
                    {
                        textBox1.FontSize = 14;

                        textBox1.Background = new SolidColorBrush(Colors.Red);

                        textBox1.Text = "Модуль загружен с ошибками : " + ex.Message;

                        if ((bool)isLogging.IsChecked)
                            Logger.WriteLine("Модуль загружен с ошибками : " + ex.Message);
                    }
                }
            }
        }

        // Метод реализующий получение имени, версии и описания подключенного модуля
        private void GetInfoAboutModule_Click(object sender, RoutedEventArgs e)
        {
            InfoAboutSystem infoAboutSystem = new();

            ICommand? module = null;

            if (modulesPathes.SelectedItem.ToString().Contains("SysInfoInternet"))
            {
                module = infoAboutSystemInternetCommands.First();
            }
            else if (modulesPathes.SelectedItem.ToString().Contains("SysInfoOS"))
            {
                module = infoAboutSystemOSCommands.First();
            }
            else if (modulesPathes.SelectedItem.ToString().Contains("SysInfoHardware"))
            {
                module = infoAboutSystemHardwareCommands.First();
            }

            if (module != null)
            {
                infoAboutSystem.infoAboutSystemText.Text = infoAboutSystem.infoAboutSystemText.Text +
                        "Название модуля : " + module.Name + "\n Описание модуля : " +
                        module.Description + "\nВерсия модуля : " + module.Version;

                if ((bool)isLogging.IsChecked)
                    Logger.WriteLine(infoAboutSystem.infoAboutSystemText.Text);

                infoAboutSystem.ShowDialog();
            }

            else
            {
                infoAboutSystem.infoAboutSystemText.Text = "Модуль загружен с ошибками";

                infoAboutSystem.ShowDialog();
            }
        }
    }

    // Класс реализующий логгирование действий, совершаемых пользователем
    static class Logger
    {
        //----------------------------------------------------------
        // Статический метод записи строки в файл лога без переноса
        //----------------------------------------------------------
        public static void Write(string text)
        {
            using (StreamWriter sw = new StreamWriter(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\log.txt", true))
            {
                sw.Write(text);
            }
        }

        //---------------------------------------------------------
        // Статический метод записи строки в файл лога с переносом
        //---------------------------------------------------------
        public static void WriteLine(string message)
        {
            using (StreamWriter sw = new StreamWriter(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\log.txt", true))
            {
                sw.WriteLine(string.Format("{0,-23} {1}", DateTime.Now.ToString() + ":", message));
            }
        }
    }

}

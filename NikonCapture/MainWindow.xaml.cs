using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Globalization;
using System.Timers;
//using System.Windows.Forms;
using Nikon; // Nikon C# Wrapper
using NikonCapture; // Nikon C# Wrapper

namespace NikonCapture
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    
    public partial class MainWindow : Window
    {
        NikonCamera Camera = new NikonCamera(); // Cria câmera da classe NikonCamera
        System.Timers.Timer TimerSegundos = new System.Timers.Timer(); // Timer usado para tirar fotos por tempo

        bool cameraDisconnecting = false; // Usada apenas para evitar que erros falso-positivos sejam gerados no log na finalização do programa
        
        /*//Teste programação
        System.Windows.Forms.Timer programacao = new System.Windows.Forms.Timer();
        bool programacaoOn = false;

        private void programacao_Tick(object sender, EventArgs e)
        {
            if (programacaoOn)
            {
                if ((DateTime.Now.Hour == Convert.ToInt32(tbTimerHora.Text)) && (DateTime.Now.Minute == Convert.ToInt32(tbTimerMinuto.Text)))
                {
                    btFoto_Click(null, null);
                    tbTimerMinuto.Text = Convert.ToString((Convert.ToInt32(tbTimerMinuto.Text)+1));
                }
            }
        }
        //Fim teste programação*/
        
        public MainWindow()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US"); // Utilizar ponto como separador decimal ao invés de vírgula
            InitializeComponent();

            /*//Teste programação
            programacao.Interval = 1;
            programacao.Tick += new EventHandler(this.programacao_Tick);
            programacao.Start();
            //Fim teste programação*/

            File.AppendAllText("log.txt", "=================\r\n= NIKON CAPTURE =\tVersão " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + "\r\n=================\r\n");
            gravaLog("Inicializando programa...");

            // Verifica a existência dos arquivos necessários
            if (File.Exists("nikoncswrapper.dll"))
	        {
		        gravaLog("Arquivo \"nikoncswrapper.dll\" encontrado!");
	        } else
	        {
                gravaLog("Arquivo \"nikoncswrapper.dll\" NÃO encontrado! Abortando...");
                miConectarCamera.IsEnabled = false;
                MessageBox.Show("O arquivo \"nikoncswrapper.dll\" não foi encontrado na pasta do programa.\n\nPor favor, coloque o arquivo na mesma pasta do executável e rode este programa novamente.","Erro");
                this.Close();
	        }
            if (File.Exists("NkdPTP.dll"))
            {
                gravaLog("Arquivo \"NkdPTP.dll\" encontrado!");
            }
            else
            {
                gravaLog("Arquivo \"NkdPTP.dll\" NÃO encontrado! Abortando...");
                miConectarCamera.IsEnabled = false;
                MessageBox.Show("O arquivo \"NkdPTP.dll\" não foi encontrado na pasta do programa.\n\nPor favor, coloque o arquivo na mesma pasta do executável e rode este programa novamente.", "Erro");
                this.Close();
            }
            // Fim da verificação dos arquivos necessários

            TimerSegundos.Elapsed += new ElapsedEventHandler(timerAtingidoFotosSegundos); // Event handler para tirar fotos por tempo
            Camera.CameraStatusChangeEvent += Camera_CameraStatusChangeEvent; // Evento para mudança de status da câmera
            Camera.CameraPhotoSaveEvent += Camera_CameraPhotoSaveEvent; // Evento para término de escrita em disco
            
        }

        // Função a ser executada quando o evento de término de escrita em disco acontece
        void Camera_CameraPhotoSaveEvent(object sender, NikonCamera.CameraPhotoSavedEventArgs e)
        {
            gravaLog("Foto " + Camera.GetLastPhotoFilename() + " gravada em disco");
            Console.WriteLine("Foto gravada em disco\t\t\t\t\t\t" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffffff"));

            tbStatus.Text = "Foto capturada";
            atualizarFrame(Camera.GetLastPhotoFilename());
        }

        // Função a ser executada quando o evento de mudança de status da câmera acontece (câmera conectada/desconectada)
        void Camera_CameraStatusChangeEvent(object sender, NikonCamera.CameraStatusChangedEventArgs e)
        {
            if (e != null)
            {
                if (e.CameraStatus) // Câmera conectada
                {
                    pbBateria.IsIndeterminate = false;
                    pbBateria.Value = e.BatteryLevel;
                    tbStatus.Text = "Câmera conectada";
                    btChoosePathToSave.IsEnabled = true;
                    //miDesconectarCamera.IsEnabled = true;
                    miConectarCamera.IsEnabled = false;
                    tbPathToSave.Text = "Escolha uma pasta";
                    cbFormatoImagem.IsEnabled = true;
                    cbAbertura.IsEnabled = true;
                    cbVelocidade.IsEnabled = true;
                    cbISO.IsEnabled = true;
                    miRestaurarPadrao.IsEnabled = true;
                    tbSegundos.Text = "0.2"; // Valor padrão - Tirar fotos a cada 0,2 segundos

                    // Muda o combo box de formato do arquivo para o correspondente na câmera
                    switch (e.FormatoArquivo)
                    {
                        case 0:
                            cbFormatoImagem.Text = "JPG Basic";
                            break;

                        case 1:
                            cbFormatoImagem.Text = "JPG Normal";
                            break;

                        case 2:
                            cbFormatoImagem.Text = "JPG Fine";
                            break;

                        case 3:
                            cbFormatoImagem.Text = "RAW";
                            break;

                        case 4:
                            cbFormatoImagem.Text = "RAW + JPG Basic";
                            break;

                        case 5:
                            cbFormatoImagem.Text = "RAW + JPG Normal";
                            break;

                        case 6:
                            cbFormatoImagem.Text = "RAW + JPG Fine";
                            break;
                        
                        default:
                            break;
                    }

                    // Muda o combo box de velocidade do obturador para o correspondente na câmera
                    switch (e.Velocidade)
                    {
                        case 47:
                            cbVelocidade.Text = "1/1000";
                            break;

                        case 48:
                            cbVelocidade.Text = "1/1250";
                            break;

                        case 49:
                            cbVelocidade.Text = "1/1600";
                            break;

                        case 50:
                            cbVelocidade.Text = "1/2000";
                            break;
                        
                        case 51:
                            cbVelocidade.Text = "1/2500";
                            break;

                        case 52:
                            cbVelocidade.Text = "1/3200";
                            break;

                        case 53:
                            cbVelocidade.Text = "1/4000";
                            break;

                        default:
                            break;
                    }

                    // Muda o combo box de abertura do obturador para o correspondente na câmera
                    switch(e.Abertura)
                    {
                        case 8:
                            cbAbertura.Text = "f/4,5";
                            break;

                        case 9:
                            cbAbertura.Text = "f/5";
                            break;

                        case 10:
                            cbAbertura.Text = "f/5,6";
                            break;

                        case 11:
                            cbAbertura.Text = "f/6,3";
                            break;

                        case 12:
                            cbAbertura.Text = "f/7,1";
                            break;

                        case 13:
                            cbAbertura.Text = "f/8";
                            break;

                        case 14:
                            cbAbertura.Text = "f/9";
                            break;

                        default:
                            break;
                    }

                    // Muda o combo box de sensibilidade ISO para o correspondente na câmera
                    switch(e.ISO)
                    {
                        case 6:
                            cbISO.Text = "200";
                            break;

                        case 7:
                            cbISO.Text = "250";
                            break;

                        case 8:
                            cbISO.Text = "320";
                            break;

                        case 9:
                            cbISO.Text = "400";
                            break;

                        case 10:
                            cbISO.Text = "500";
                            break;

                        case 11:
                            cbISO.Text = "640";
                            break;

                        case 12:
                            cbISO.Text = "800";
                            break;

                        default:
                            break;
                    }
                    

                    gravaLog("Câmera conectada");
                }
                else // Câmera desconectada
                {
                    pbBateria.IsIndeterminate = true;
                    pbBateria.Value = 0;
                    tbStatus.Text = "Câmera desconectada";
                    tbSegundos.IsEnabled = false;
                    btChoosePathToSave.IsEnabled = false;
                    //miDesconectarCamera.IsEnabled = false;
                    miConectarCamera.IsEnabled = true;

                    cbFormatoImagem.Text = "";
                    cbFormatoImagem.IsEnabled = false;
                    cbAbertura.Text = "";
                    cbAbertura.IsEnabled = false;
                    cbVelocidade.Text = "";
                    cbVelocidade.IsEnabled = false;
                    cbISO.Text = "";
                    cbISO.IsEnabled = false;
                    miRestaurarPadrao.IsEnabled = false;

                    gravaLog("Câmera desconectada");

                }
                

                //tbSegundos.Text = Convert.ToString(imPhoto.Source);
            }
            
        }
        
        // Roda ao fechar o programa
        private void Main_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            gravaLog("Desconectando câmera...");
            Console.WriteLine("Desconectando câmera...");

            cameraDisconnecting = true;

            cbFormatoImagem.IsEnabled = false;
            tbSegundos.IsEnabled = false;
            btChoosePathToSave.IsEnabled = false;
            btFoto.IsEnabled = false;
            btStartStopInterval.IsEnabled = false;

            try
            {
                Camera.DesconectarCamera(); // Desligar câmera
            }
            catch (Exception ex)
            {
                gravaLog("Erro ao tentar desconectar câmera: " + ex);
            }
            

            gravaLog("Fechando...\r\n");
        }
        
        // Botão para tirar uma única foto
        private void btFoto_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Tirar o dedo do botão\t\t\t\t\t\t" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffffff"));
            gravaLog("Efetuando único disparo...");
            //Console.WriteLine("Capturando foto");
            //tbStatus.Text = "Capturando foto...";
            Camera.CapturarFoto();
            pbBateria.Value = Camera.NivelBateria();
            
        }

        public void gravaLog(string m)
        {
            File.AppendAllText("log.txt", "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffffff") + "]\t" + m + "\r\n");
            return;
        }

        /*
        // Botão para iniciar captura por intervalo
        private void btFotoSegundos_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double intervalo = Convert.ToDouble(tbSegundos.Text);

                Console.WriteLine("Iniciando captura de sequência");
                tbStatus.Text = "Iniciando captura de sequência...";
                gravaLog("Tempo entre cada disparo configurado para " + intervalo + " segundo(s)");
                gravaLog("Iniciando gravação por intervalo...");

                btFotoSegundos.IsEnabled = false;
                btFoto.IsEnabled = false;
                btPararSequencia.IsEnabled = true;
                tbSegundos.IsEnabled = false;
                btChoosePathToSave.IsEnabled = false;
                miDesconectarCamera.IsEnabled = false;

                TimerSegundos.Interval = intervalo * 1000;
                TimerSegundos.Enabled = true;
                Camera.CapturarFoto();

            }
            catch
            {
                MessageBox.Show("Verifique se o valor do segundo está correto","Erro encontrado");
                gravaLog("Erro ao iniciar gravação por intervalo. Tempo inválido.");
                tbSegundos.Text = "";
            }
                        
        }
         * */

        // Função a ser executada quando o intervalo de tempo para tirar foto é atingido
        private void timerAtingidoFotosSegundos(object sender, ElapsedEventArgs e)
        {
            //Console.WriteLine("Timer alcançado");
            //tbStatus.Text = "Capturando próxima foto da sequência..."; // Essa linha impede a execução do código
            
            Camera.CapturarFoto();
            pbBateria.Value = Camera.NivelBateria();
            
        }

        /*
        // Botão para parar captura de foto por intervalo
        private void btPararSequencia_Click(object sender, RoutedEventArgs e)
        {
            btFotoSegundos.IsEnabled = true;
            btPararSequencia.IsEnabled = false;
            btFoto.IsEnabled = true;
            tbSegundos.IsEnabled = true;
            btChoosePathToSave.IsEnabled = true;
            miDesconectarCamera.IsEnabled = true;
            
            TimerSegundos.Enabled = false;
            
            Console.WriteLine("Parando captura de sequencia");
            tbStatus.Text = "Captura de sequência interrompida";
            gravaLog("Parando gravação por intervalo");
        }
         * */

        private void pbBateria_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (pbBateria.Value < 40)
            {
                pbBateria.Foreground = Brushes.Red;
            }
            else
            {
                //var azul = new SolidColorBrush(Color.FromRgb(51, 153, 255)); // #FF3399FF - Cor original
                pbBateria.Foreground = Brushes.Green; 
            }
        }

        private void miSair_Click(object sender, RoutedEventArgs e)
        {
            tbStatus.Text = "Saindo, aguarde...";
            //Thread.Sleep(500);
            //Application.DoEvents();
            Console.WriteLine("Saindo");
            gravaLog("Encerrando programa...");
            this.Close();
        }

        private void miConectarCamera_Click(object sender, RoutedEventArgs e)
        {
            tbStatus.Text = "Buscando câmera...";
            gravaLog("Aguardando conexão com a câmera...");
            pbBateria.IsIndeterminate = true;

            try
            {
                // Janela para escolher arquivo MD3
                var dialog = new System.Windows.Forms.OpenFileDialog();
                dialog.Filter = "Nikon SDK MD3 Files (*.md3)|*.md3";
                dialog.InitialDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
                dialog.ShowDialog();
                if (dialog.FileName != "")
                {
                    gravaLog("Carregando arquivo " + dialog.SafeFileName + "...");
                    Camera.ConectarCamera(dialog.FileName);

                }
                else
                {
                    gravaLog("Carregamento de arquivo MD3 abortado");
                    pbBateria.IsIndeterminate = false;
                    tbStatus.Text = "Pronto";
                }
                
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro: " + ex, "Erro");
                gravaLog("ERRO! " + ex);
                pbBateria.IsIndeterminate = false;
                tbStatus.Text = "Pronto";
            }

            
        }

        /*
        private void miDesconectarCamera_Click(object sender, RoutedEventArgs e)
        {
            tbStatus.Text = "Desconectando câmera";
            gravaLog("Desconectando câmera...");

            
            
            pbBateria.Value = 0;
            pbBateria.IsIndeterminate = false;
            pbBateria.Foreground = Brushes.Green

            Camera.DesconectarCamera();
        }
         */

        private void atualizarFrame(string filename)
        {
            
            tbNomeFoto.Text = filename;

            try
            {
                string url;
                if (filename.Contains(".nef"))
                {
                    Console.WriteLine("Formato NEF detectado! Prévia não disponível.");
                    Uri uri = new Uri(@"blank.jpg", UriKind.RelativeOrAbsolute);
                    ImageSource imagem = new BitmapImage(uri);
                    imPhoto.Source = imagem;
                    
                }
                else
                {
                    Console.WriteLine("Formato JPG detectado! Mostrando prévia...");
                    url = System.IO.Path.Combine(tbPathToSave.Text, filename);
                    Console.WriteLine(url);
                    Uri uri = new Uri("file://" + url);
                    ImageSource imagem = new BitmapImage(uri);
                    imPhoto.Source = imagem;
                }
                
            }
            catch
            {
                Console.WriteLine("Erro ao mostrar prévia");
                //throw;
            }
            
            
            
        }

        private void miSobre_Click(object sender, RoutedEventArgs e)
        {
            string v = "v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            string m = ("Nikon Capture " + v + "\nBASE Aerofotogrametria e Projetos S.A.\n\nProgramação: Henrique Germano Miraldo\n\nUtilizando Nikon SDK C# Wrapper v2.0.1 por tdideriksen\nhttp://sourceforge.net/projects/nikoncswrapper");
            
            MessageBox.Show(m,"Sobre");
        }

        private void miVerLog_Click(object sender, RoutedEventArgs e)
        {
            
            if (File.Exists("log.txt"))
            {
                System.Diagnostics.Process.Start("log.txt");
            }
            else
            {
                MessageBox.Show("Nenhum log no momento.");
            }
            
        }

        private void btChoosePathToSave_Click(object sender, RoutedEventArgs e)
        {
            //Camera.pathToSave = "output";

            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.ShowDialog();

            if (dialog.SelectedPath != "")
            {
                Console.WriteLine("Selecionada pasta: {0}", dialog.SelectedPath);
                gravaLog("Pasta de saída selecionada: " + dialog.SelectedPath);
                Camera.pathToSave = dialog.SelectedPath;
                tbPathToSave.Text = dialog.SelectedPath;
                tbSegundos.IsEnabled = true;
                btFoto.IsEnabled = true;
                btStartStopInterval.IsEnabled = true;

                string metaPath = System.IO.Path.Combine(dialog.SelectedPath, "MetaNikon.txt");
                File.AppendAllText(metaPath,"DATE\tPRE-TIME\tPRE-MS\tPOST-TIME\tPOST-MS\tREC-TIME\tREC-MS\tFILENAME\r\n");
            }
            else
            {
                Console.WriteLine("Pasta não selecionada");
            }
        }

        private void btStartStopInterval_Click(object sender, RoutedEventArgs e)
        {
            if (!TimerSegundos.Enabled) // Código para iniciar o timer
            {
                try
                {
                    double intervalo = Convert.ToDouble(tbSegundos.Text);

                    Console.WriteLine("Iniciando captura de sequência");
                    tbStatus.Text = "Iniciando captura de sequência...";
                    gravaLog("Tempo entre cada disparo configurado para " + intervalo + " segundo(s)");
                    gravaLog("Iniciando gravação por intervalo...");

                    btFoto.IsEnabled = false;
                    tbSegundos.IsEnabled = false;
                    btChoosePathToSave.IsEnabled = false;
                    //miDesconectarCamera.IsEnabled = false;
                    btStartStopInterval.Content = "Parar timer";

                    TimerSegundos.Interval = intervalo * 1000;
                    TimerSegundos.Enabled = true;
                    Camera.CapturarFoto();

                }
                catch
                {
                    MessageBox.Show("Verifique se o valor do segundo está correto", "Erro encontrado");
                    gravaLog("Erro ao iniciar gravação por intervalo. Tempo inválido.");
                    tbSegundos.Text = "";
                }
            }
            else // Código para parar o timer
            {
                btFoto.IsEnabled = true;
                tbSegundos.IsEnabled = true;
                btChoosePathToSave.IsEnabled = true;
                //miDesconectarCamera.IsEnabled = true;
                btStartStopInterval.Content = "Iniciar timer";

                TimerSegundos.Enabled = false;

                Console.WriteLine("Parando captura de sequencia");
                tbStatus.Text = "Captura de sequência interrompida";
                gravaLog("Parando gravação por intervalo");
            }
        }

        // Função ao mudar a seleção do combo box da qualidade de imagem
        private void cbFormatoImagem_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbFormatoImagem.IsEnabled) // Apenas fazer algo caso a combobox estiver ativa
            {
                // 0 = JPG Basic
                // 1 = JPG Normal
                // 2 = JPG Fine
                // 3 = RAW
                // 4 = RAW + JPG Basic
                // 5 = RAW + JPG Normal
                // 6 = RAW + JPG Fine

                try
                {
                    switch (((ComboBoxItem)cbFormatoImagem.SelectedItem).Content.ToString())
                    {
                        case "JPG Basic":
                            Camera.MudarFormatoArquivo(0);
                            gravaLog("Formato de arquivo alterado para: JPG Basic");
                            break;

                        case "JPG Normal":
                            Camera.MudarFormatoArquivo(1);
                            gravaLog("Formato de arquivo alterado para: JPG Normal");
                            break;

                        case "JPG Fine":
                            Camera.MudarFormatoArquivo(2);
                            gravaLog("Formato de arquivo alterado para: JPG Fine");
                            break;

                        case "RAW":
                            Camera.MudarFormatoArquivo(3);
                            gravaLog("Formato de arquivo alterado para: RAW");
                            break;

                        case "RAW + JPG Basic":
                            Camera.MudarFormatoArquivo(4);
                            gravaLog("Formato de arquivo alterado para: RAW + JPG Basic");
                            break;

                        case "RAW + JPG Normal":
                            Camera.MudarFormatoArquivo(5);
                            gravaLog("Formato de arquivo alterado para: RAW + JPG Normal");
                            break;

                        case "RAW + JPG Fine":
                            Camera.MudarFormatoArquivo(6);
                            gravaLog("Formato de arquivo alterado para: RAW + JPG Fine");
                            break;

                        default:
                            gravaLog("Escolha de formato de arquivo inválida");
                            break;
                    }
                    
                }
                catch (Exception ex)
                {
                    if (!cameraDisconnecting) gravaLog("Erro ao tentar mudar o formato do arquivo: " + ex);
                }

                
            }
        }

        // Função ao mudar a seleção do combo box da abertura 
        private void cbAbertura_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbAbertura.IsEnabled) // Apenas fazer algo caso a combobox estiver ativa
            {
                // 8 = f/4,5
                // 9 = f/5
                // 10 = f/5,6
                // 11 = f/6,3
                // 12 = f/7,1
                // 13 = f/8
                // 14 = f/9

                try
                {
                    switch (((ComboBoxItem)cbAbertura.SelectedItem).Content.ToString())
                    {
                        case "f/4,5":
                            Camera.MudarAberturaObturador(8);
                            gravaLog("Abertura do obturador alterado para: f/4,5");
                            break;

                        case "f/5":
                            Camera.MudarAberturaObturador(9);
                            gravaLog("Abertura do obturador alterado para: f/5");
                            break;

                        case "f/5,6":
                            Camera.MudarAberturaObturador(10);
                            gravaLog("Abertura do obturador alterado para: f/5,6");
                            break;

                        case "f/6,3":
                            Camera.MudarAberturaObturador(11);
                            gravaLog("Abertura do obturador alterado para: f/6,3");
                            break;

                        case "f/7,1":
                            Camera.MudarAberturaObturador(12);
                            gravaLog("Abertura do obturador alterado para: f/7,1");
                            break;

                        case "f/8":
                            Camera.MudarAberturaObturador(13);
                            gravaLog("Abertura do obturador alterado para: f/8");
                            break;

                        case "f/9":
                            Camera.MudarAberturaObturador(14);
                            gravaLog("Abertura do obturador alterado para: f/9");
                            break;

                        default:
                            gravaLog("Escolha de abertura do obturador inválida");
                            break;
                    }

                }
                catch (Exception ex)
                {
                    if (!cameraDisconnecting) gravaLog("Erro ao tentar mudar a abertura do obturador: " + ex);
                }


            }
        }

        private void cbVelocidade_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbVelocidade.IsEnabled) // Apenas fazer algo caso a combobox estiver ativa
            {
                // 47 = 1/1000
                // 48 = 1/1250
                // 49 = 1/1600
                // 50 = 1/2000
                // 51 = 1/2500
                // 52 = 1/3200
                // 53 = 1/4000

                try
                {
                    switch (((ComboBoxItem)cbVelocidade.SelectedItem).Content.ToString())
                    {
                        case "1/1000":
                            Camera.MudarVelocidadeObturador(47);
                            gravaLog("Velocidade do obturador alterada para: 1/1000");
                            break;

                        case "1/1250":
                            Camera.MudarVelocidadeObturador(48);
                            gravaLog("Velocidade do obturador alterada para: 1/1250");
                            break;

                        case "1/1600":
                            Camera.MudarVelocidadeObturador(49);
                            gravaLog("Velocidade do obturador alterada para: 1/1600");
                            break;

                        case "1/2000":
                            Camera.MudarVelocidadeObturador(50);
                            gravaLog("Velocidade do obturador alterada para: 1/2000");
                            break;

                        case "1/2500":
                            Camera.MudarVelocidadeObturador(51);
                            gravaLog("Velocidade do obturador alterada para: 1/2500");
                            break;

                        case "1/3200":
                            Camera.MudarVelocidadeObturador(52);
                            gravaLog("Velocidade do obturador alterada para: 1/3200");
                            break;

                        case "1/4000":
                            Camera.MudarVelocidadeObturador(53);
                            gravaLog("Velocidade do obturador alterada para: 1/4000");
                            break;

                        default:
                            gravaLog("Escolha de velocidade do obturador inválida");
                            break;
                    }

                }
                catch (Exception ex)
                {
                    if (!cameraDisconnecting) gravaLog("Erro ao tentar mudar a velocidade do obturador: " + ex);
                }


            }
        }

        private void cbISO_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbISO.IsEnabled) // Apenas fazer algo caso a combobox estiver ativa
            {
                // 6 = 200
                // 7 = 250
                // 8 = 320
                // 9 = 400
                // 10 = 500
                // 11 = 640
                // 12 = 800

                try
                {
                    switch (((ComboBoxItem)cbISO.SelectedItem).Content.ToString())
                    {
                        case "200":
                            Camera.MudarSensibilidadeISO(6);
                            gravaLog("Sensibilidade ISO alterada para: 200");
                            break;

                        case "250":
                            Camera.MudarSensibilidadeISO(7);
                            gravaLog("Sensibilidade ISO alterada para: 250");
                            break;

                        case "320":
                            Camera.MudarSensibilidadeISO(8);
                            gravaLog("Sensibilidade ISO alterada para: 320");
                            break;

                        case "400":
                            Camera.MudarSensibilidadeISO(9);
                            gravaLog("Sensibilidade ISO alterada para: 400");
                            break;

                        case "500":
                            Camera.MudarSensibilidadeISO(10);
                            gravaLog("Sensibilidade ISO alterada para: 500");
                            break;

                        case "640":
                            Camera.MudarSensibilidadeISO(11);
                            gravaLog("Sensibilidade ISO alterada para: 640");
                            break;

                        case "800":
                            Camera.MudarSensibilidadeISO(12);
                            gravaLog("Sensibilidade ISO alterada para: 800");
                            break;

                        default:
                            gravaLog("Escolha de sensibilidade ISO inválida");
                            break;
                    }

                }
                catch (Exception ex)
                {
                    if (!cameraDisconnecting) gravaLog("Erro ao tentar mudar a sensibilidade ISO: " + ex);
                }


            }
        }

        private void miInstrucoes_Click(object sender, RoutedEventArgs e)
        {
            string m1 = "1) Clique em 'Arquivo' > 'Carregar câmera' e abra o arquivo MD3\nreferente à câmera Nikon que você deseja utilizar.\n\n";
            string m2 = "2) O programa irá buscar pela câmera e, se tudo correr bem, você\ndeverá ver a palavra 'Pronto' na barra de status.\n\n";
            string m3 = "3) Clique no botão '...' para escolher uma pasta onde as imagens\ndeverão ser salvas. Certifique-se de possuir espaço em disco\nsuficiente.\n\n";
            string m4 = "4) Clique no botão 'Tirar foto única' ou digite um valor (em\nsegundos) e clique em 'Iniciar timer' para tirar fotos por intervalo\nde tempo. Para interromper a captura, clique em 'Parar timer'.\n\n";
            string m5 = "5) As configurações da câmera podem ser modificadas e aplicadas\nsem a necessidade de interromper a captura por intervalo\n\n";
            string m6 = "6) Evite desconectar a câmera enquanto o programa está em\nexecução. Em caso de problemas, reinicie o computador";
            string m = m1 + m2 + m3 + m4 + m5 + m6;
            MessageBox.Show(m, "Instruções");
        }

        private void miRestaurarPadrao_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                gravaLog("Aplicando configuração padrão... JPG Fine - 1/2500 - f/6,3 - ISO 400");
                Camera.MudarFormatoArquivo(2);
                cbFormatoImagem.Text = "JPG Fine";
                Camera.MudarAberturaObturador(11);
                cbAbertura.Text = "f/6,3";
                Camera.MudarVelocidadeObturador(51);
                cbVelocidade.Text = "1/2500";
                Camera.MudarSensibilidadeISO(9);
                cbISO.Text = "400";
                gravaLog("Configuração padrão aplicada");
            }
            catch (Exception ex)
            {
                gravaLog("Erro restaurando configurações padrão: " + ex);
            }
        }

        /*//Teste programação
        private void btStartProgramacao_Click(object sender, RoutedEventArgs e)
        {
            programacaoOn = true;
        }
        //Fim teste programação*/
    }
}

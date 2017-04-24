using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Timers;
using Nikon; // Nikon C# Wrapper
using NikonCapture; // Nikon C# Wrapper
using System.IO;

namespace NikonCapture
{
    class NikonCamera
    {
        NikonManager Manager;
        NikonDevice CameraDevice;
        string LastFilename;

        public bool statusCameraConectada { get; set; }
        public string pathToSave { get; set; }
        //public string formatoArquivo { get; set; }
                
        //int sequencia = 0;
        bool metaFinishOK = true;
        
        public void ConectarCamera(string md3SDKFile)
        {
            // Criar objeto manager
            
            NikonManager man = new NikonManager(md3SDKFile); // SDK para Nikon
            Manager = man;
            Console.WriteLine("Manager criado");
            

            // Listen for the 'DeviceAdded' event
            Manager.DeviceAdded += manager_DeviceAdded;
            Manager.DeviceRemoved += Manager_DeviceRemoved;
            
            
        }
        /*
        private int GetBateryLevel()
        {

        }
        */
        void Manager_DeviceRemoved(NikonManager sender, NikonDevice device)
        {
            Manager.Shutdown();
            Console.WriteLine("Dispositivo removido");
            statusCameraConectada = false;
            OnCameraStatusChanged(new CameraStatusChangedEventArgs(statusCameraConectada, 100, 2, 51, 12, 9)); // Pode ser qualquer valor, mas deixei aqui os padrões para o voo:
            // Bateria: 100
            // Tipo do arquivo: 2 - JPEG Fine
            // Velocidade: 51 - 1/2500
            // Abertura: 12 - 6,3
            // ISO: 9 - 400
        }

        
        public void DesconectarCamera()
        {
            try
            {
                Manager.Shutdown();
                Console.WriteLine("Desligando câmera");
            }
            catch
            {
                Console.WriteLine("Câmera já desconectada");
            }
            
            statusCameraConectada = false;
            OnCameraStatusChanged(new CameraStatusChangedEventArgs(statusCameraConectada, 100, 2, 51, 12, 9)); // Pode ser qualquer valor, mas deixei aqui os padrões para o voo:
            // Bateria: 100
            // Tipo do arquivo: 2 - JPEG Fine
            // Velocidade: 51 - 1/2500
            // Abertura: 12 - 6,3
            // ISO: 9 - 400
        }


        // Função a ser executada quando a câmera tirar uma foto
        public void CapturarFoto()
        {
            try
            {
                //Console.WriteLine("Início da captura:                " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffffff"));
                
                if (metaFinishOK)
                {
                    metaFinishOK = false;

                    string metaPath = Path.Combine(pathToSave, "MetaNikon.txt");
                    DateTime timeStamp = DateTime.Now;
                    string meta = timeStamp.ToString("yyyyMMdd\tHHmmss\tfffffff\t"); // Instante antes do disparo do obturador
                    File.AppendAllText(metaPath, meta);

                    CameraDevice.Capture();


                    DateTime timeStamp2 = DateTime.Now;
                    string meta2 = timeStamp2.ToString("HHmmss\tfffffff\t"); // Instante depois do disparo do obturador
                    File.AppendAllText(metaPath, meta2);
                    //Console.WriteLine("Fim da captura:                   " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffffff"));

                }
                else
                {
                    Console.WriteLine("Foto anterior não terminou de ser gravada! Ignorando disparo...");

                }
                
                
            }
            catch (NikonException ex)
            {
                Console.WriteLine("Erro ao capturar foto: " + ex);
                metaFinishOK = true;
            }
        }

        // Função a ser executada quando a imagem estiver pronta para ser salva em disco
        void cameraDevice_ImageReadyToSave(NikonDevice sender, NikonImage image)
        {
            // Save captured image to disk
            //sequencia++;

            // Prepara imagem para gravação em disco
            string filename = DateTime.Now.ToString("yyyyMMdd") + "_" + DateTime.Now.ToString("HHmmss") + "d" + DateTime.Now.ToString("ff") /*+ sequencia.ToString("0000")*/ + ((image.Type == NikonImageType.Jpeg) ? ".jpg" : ".nef");
            LastFilename = filename;
            string fullPath = Path.Combine(pathToSave, filename);

            // Prepara metadados para gravação no arquivo Meta
            string metaPath = Path.Combine(pathToSave, "MetaNikon.txt");
            DateTime timeStamp = DateTime.Now;
            string meta = timeStamp.ToString("HHmmss\tfffffff\t") + filename + "\r\n"; // Instante antes da gravação em disco da imagem
            File.AppendAllText(metaPath, meta);

            metaFinishOK = true;

            // Grava a imagem no disco
            using (FileStream s = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
            {
                //Console.WriteLine("Início da gravação da " + filename + "\t\t" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffffff"));
                s.Write(image.Buffer, 0, image.Buffer.Length);
                //Console.WriteLine("Final da gravação da " + filename + "\t\t" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffffff"));
            }
            
            OnCameraPhotoSaved(new CameraPhotoSavedEventArgs(filename));

            
        }

        
        public int NivelBateria()
        {
            try
            {
                
                return CameraDevice.GetInteger(eNkMAIDCapability.kNkMAIDCapability_BatteryLevel);
                
            }
            catch (NikonException ex)
            {
                Console.WriteLine("Erro ao ler nível da bateria: " + ex);
                return 0;
            }
        }

        // Função que é executada quando uma câmera é detectada
        void manager_DeviceAdded(NikonManager sender, NikonDevice device)
        {
            try
            {
                if (CameraDevice == null)
                {
                    // Save device
                    CameraDevice = device;
                    Console.WriteLine("Dispositivo detectado");
                    //MainWindow
                    CameraDevice.ImageReady += cameraDevice_ImageReadyToSave;
                    int batLevel = CameraDevice.GetInteger(eNkMAIDCapability.kNkMAIDCapability_BatteryLevel); // Lê nível da bateria
                    int formatoArq = (CameraDevice.GetEnum(eNkMAIDCapability.kNkMAIDCapability_CompressionLevel)).Index; // Lê formato do arquivo
                    int velObturador = (CameraDevice.GetEnum(eNkMAIDCapability.kNkMAIDCapability_ShutterSpeed)).Index; // Lê velocidade do obturador
                    int abertObturador = (CameraDevice.GetEnum(eNkMAIDCapability.kNkMAIDCapability_Aperture)).Index; // Lê abertura do obturador
                    int sensibIso = (CameraDevice.GetEnum(eNkMAIDCapability.kNkMAIDCapability_Sensitivity)).Index; // Lê sensibilidade ISO
                    statusCameraConectada = true;
                    OnCameraStatusChanged(new CameraStatusChangedEventArgs(statusCameraConectada, batLevel, formatoArq, velObturador, abertObturador, sensibIso));
                    
                }
            }
            catch (NikonException ex)
            {
                Console.WriteLine("Erro ao detectar dispositivo: " + ex);
            }            
        }

        public string GetLastPhotoFilename()
        {
            return LastFilename;
        }

        // Função para mudar o formato do arquivo
        public void MudarFormatoArquivo(int i)
        {
            NikonEnum a = CameraDevice.GetEnum(eNkMAIDCapability.kNkMAIDCapability_CompressionLevel);
            a.Index = i;
            CameraDevice.SetEnum(eNkMAIDCapability.kNkMAIDCapability_CompressionLevel, a);
        }

        // Função para mudar a velocidade do obturador
        public void MudarVelocidadeObturador(int i)
        {
            NikonEnum a = CameraDevice.GetEnum(eNkMAIDCapability.kNkMAIDCapability_ShutterSpeed);
            a.Index = i;
            CameraDevice.SetEnum(eNkMAIDCapability.kNkMAIDCapability_ShutterSpeed, a);
        }

        // Função para mudar a abertura do obturador
        public void MudarAberturaObturador(int i)
        {
            NikonEnum a = CameraDevice.GetEnum(eNkMAIDCapability.kNkMAIDCapability_Aperture);
            a.Index = i;
            CameraDevice.SetEnum(eNkMAIDCapability.kNkMAIDCapability_Aperture, a);
        }

        // Função para mudar a sensibilidade ISO
        public void MudarSensibilidadeISO(int i)
        {
            NikonEnum a = CameraDevice.GetEnum(eNkMAIDCapability.kNkMAIDCapability_Sensitivity);
            a.Index = i;
            CameraDevice.SetEnum(eNkMAIDCapability.kNkMAIDCapability_Sensitivity, a);
        }


        // --- Início do evento para quando o status da câmera mudar

        public event CameraStatusChangedEventHandler CameraStatusChangeEvent;

        public virtual void OnCameraStatusChanged(CameraStatusChangedEventArgs e)
        {
            CameraStatusChangedEventHandler handler = CameraStatusChangeEvent;
            if (handler != null)
                handler(this, e);
        }

        public delegate void CameraStatusChangedEventHandler(object sender, CameraStatusChangedEventArgs e);

        public class CameraStatusChangedEventArgs : EventArgs
        {
            private bool cameraStatus;
            private int batteryLevel;
            private int formArquivo;
            private int velObturador;
            private int abertObturador;
            private int sensibIso;
            
            public CameraStatusChangedEventArgs(bool status, int bat, int formArq, int vel, int abert, int senIso)
            {
                cameraStatus = status;
                batteryLevel = bat;
                formArquivo = formArq;
                velObturador = vel;
                abertObturador = abert;
                sensibIso = senIso;
            }

            public bool CameraStatus { get { return cameraStatus; } }
            public int BatteryLevel { get { return batteryLevel; } }
            public int FormatoArquivo { get { return formArquivo; } }
            public int Velocidade { get { return velObturador; } }
            public int Abertura { get { return abertObturador; } }
            public int ISO { get { return sensibIso; } }
        }

        // --- Fim do evento para quando o status da câmera mudar

        // --- Início do evento para quando a câmera terminar de salvar imagem

        public event CameraPhotoSavedEventHandler CameraPhotoSaveEvent;

        public virtual void OnCameraPhotoSaved(CameraPhotoSavedEventArgs e)
        {
            CameraPhotoSavedEventHandler handler = CameraPhotoSaveEvent;
            if (handler != null)
                handler(this, e);
        }

        public delegate void CameraPhotoSavedEventHandler(object sender, CameraPhotoSavedEventArgs e);

        public class CameraPhotoSavedEventArgs : EventArgs
        {
            private string filename;

            public CameraPhotoSavedEventArgs(string file)
            {
                filename = file;
            }

            public string Filename { get { return filename; } }
        }

        // --- Fim do evento para quando a câmera terminar de salvar imagem

        // --- Início do evento para atualização da bateria

        public event CameraBateryUpdatedEventHandler CameraBateryUpdateEvent;

        public virtual void OnCameraBateryUpdated(CameraBateryUpdatedEventArgs e)
        {
            CameraBateryUpdatedEventHandler handler = CameraBateryUpdateEvent;
            if (handler != null)
                handler(this, e);
        }

        public delegate void CameraBateryUpdatedEventHandler(object sender, CameraBateryUpdatedEventArgs e);

        public class CameraBateryUpdatedEventArgs : EventArgs
        {
            private int batery;

            public CameraBateryUpdatedEventArgs(int batt)
            {
                batery = batt;
            }

            public int BateryLevel { get { return batery; } }
        }

        // --- Fim do evento para atualização da bateria

        

    }
}

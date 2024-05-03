using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using Sharp7;

namespace DataCollectorService
{
    public partial class Service : ServiceBase
    {
        private readonly Thread workerThread;

        private bool QueryIsActive = false; //Флаг активности запроса
        private static S7Client Client;
        private System.Timers.Timer timer;
        private DateTime CurrentDate;
        private readonly byte[] dbBuffer = new byte[16]; //Буфер данных контроллера 
        private readonly int Rack = 0, Slot = 1; // default for S71x00
        // private readonly int Rack = 0, Slot = 2; // default for S7300
        private readonly EventLog eventLog = new EventLog
        {
            Source = "DataCollector",
            Log = "DataCollector"
        };
        private readonly SqlConnection dbConn = new SqlConnection();
        private string sDBConnectionString, sControllerIP1, sControllerIP2, sControllerIP3;
        private double TimerInterval;
        struct BriquettesData
        {
            public int result;
            // 0.0 Ошибка
            public bool Error;
            // 0.1 Пресс выключен
            public bool PressOff;
            // 0.2 Ручной режим
            public bool ManualMode;
            // 0.3 Авто режим
            public bool AutoMode;
            // 2.0 Счетчик брикетов DInt
            public long Counter;
            // 6.0 Моточасы DInt
            public long MotorHours;
            // 10.0 Safety
            public bool Safety;
            // 10.1 Фильтр загрязнен
            public bool DirtyFilter;
            // 10.2 Уровень масла
            public bool OilLevel;
            // 10.3 Масло меньше 15
            public bool OilLess15;
            // 10.4 Масло выше 75
            public bool OilMore75;
            // 10.5 Ошибка датчика давления
            public bool PressureSensorError;
            // 10.6 Ошибка датчика температуры
            public bool TempSensorError;
            // 10.7 Цилиндры не в исходном положении
            public bool CylindersNotInHome;
            // 11.0 Главный цилиндр не достигает заданного давления
            public bool MainCylinderPressureError;
            // 11.1 Вертикальный цилиндр не достигает заданного давления
            public bool VerticalCylinderPressureError;
            // 11.2 Цилиндр формы не достигает заданного давления
            public bool FormCylinderPressureError;
            // 11.3 Произвольный ход главного цилиндра
            public bool FreeStrokeMainCylinder;
            // 11.4 Произвольный ход вертикального цилиндра
            public bool FreeStrokeVerticalCylinder;
            // 11.5 Произвольный ход цилиндра формы
            public bool FreeStrokeFormCylinder;
            // 11.6 Мало опилок
            public bool NoSawdust;
            // 11.7 LifeBit
            public bool Lifebit;
        }
        public Service()
        {
            InitializeComponent();

            try
            {
                if (!EventLog.SourceExists("DataCollector"))
                    EventLog.CreateEventSource("DataCollector", "DataCollector");
            }
            catch (Exception exdb)
            {
                AddLogError("Ошибка создания журнала событий. Первый раз запустите программу от имени администратора\n" + exdb.Message);
            }

            workerThread = new Thread(DoWork);
            workerThread.SetApartmentState(ApartmentState.STA);

        }
        public void Start()
        {
            workerThread.Start();
        }
        public new void Stop()
        {
            timer.Stop();
            Client.Disconnect();
            dbConn.Close();
            AddLog("Соединение с контроллером разорвано.");
            workerThread.Abort();
        }
        protected override void OnStart(string[] args)
        {
            Start();
        }
        protected override void OnStop()
        {

            Stop();
        }
        private void DoWork()
        {
            try
            {
                // Считывание х32 приложением х64 реестра 

                using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                {
                    using (var subKey = baseKey.OpenSubKey(@"SOFTWARE\DataCollector", RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.FullControl))
                    {
                        if (subKey != null)
                        {
                            sDBConnectionString = (string)subKey.GetValue("DBConnectionString");
                            sControllerIP1 = (string)subKey.GetValue("ControllerIP1");
                            sControllerIP2 = (string)subKey.GetValue("ControllerIP2");
                            sControllerIP3 = (string)subKey.GetValue("ControllerIP3");
                            double.TryParse(subKey.GetValue("TimerInterval").ToString(), out TimerInterval);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AddLogError("Ошибка чтения настроек из реестра " + ex.Message);
            }
            timer = new System.Timers.Timer
            {
                Enabled = true,
                Interval = 100
            };
            timer.Elapsed += new System.Timers.ElapsedEventHandler(Timer_Elapsed);
            timer.AutoReset = true;
        }
        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            timer.Interval = TimerInterval;
            if (!QueryIsActive)
            {
                Client = new S7Client();
                CurrentDate = DateTime.Now;
                BriquettesData BriqData = new BriquettesData();
                try
                {
                    QueryIsActive = true;
                    try
                    {
                        dbConn.ConnectionString = sDBConnectionString;
                        dbConn.Open();
                        AddLog("Соединение с базой данных установлено.");
                    }
                    catch (Exception ex)
                    {
                        AddLogError("Ошибка соединения с базой данных: " + ex.Message);
                    }

                    if (PlcConnect(sControllerIP1, Rack, Slot))
                    {
                        BriqData = ReadData();
                        Client.Disconnect();
                        if (BriqData.result == 0)
                            WriteToDB("Press1Data", BriqData);
                    } else
                        AddLogError("Ошибка соединения с контроллером пресса 1");

                    if (PlcConnect(sControllerIP2, Rack, Slot))
                    {
                        BriqData = ReadData();
                        Client.Disconnect();
                        if (BriqData.result == 0)
                            WriteToDB("Press2Data", BriqData);
                    } else
                        AddLogError("Ошибка соединения с контроллером пресса 2");

                    if (PlcConnect(sControllerIP3, Rack, Slot))
                    {
                        BriqData = ReadData();
                        Client.Disconnect();
                        if (BriqData.result == 0)
                            WriteToDB("Press3Data", BriqData);
                    } else
                        AddLogError("Ошибка соединения с контроллером пресса 3");
                }
                catch (Exception ex)
                {
                    AddLogError("Ошибка соединения: " + ex.Message);
                }
                finally
                {
                    QueryIsActive = false;
                    Client.Disconnect();
                    dbConn.Close();
                }
            }
            else
                AddLog("Query is Active");
        }

        private BriquettesData ReadData()
        {
            BriquettesData BriqData = new BriquettesData();
            //   Читаем данные из контроллера
            BriqData.result = Client.DBRead(13, 0, 12, dbBuffer);
            if (BriqData.result == 0)
            {
                BriqData.Error = S7.GetBitAt(dbBuffer, 0, 0);
                BriqData.PressOff = S7.GetBitAt(dbBuffer, 0, 1);
                BriqData.ManualMode = S7.GetBitAt(dbBuffer, 0, 2);
                BriqData.AutoMode = S7.GetBitAt(dbBuffer, 0, 3);
                BriqData.Counter = S7.GetDIntAt(dbBuffer, 2);
                BriqData.MotorHours = S7.GetDIntAt(dbBuffer, 6);
                BriqData.Safety = S7.GetBitAt(dbBuffer, 10, 0);
                BriqData.DirtyFilter = S7.GetBitAt(dbBuffer, 10, 1);
                BriqData.OilLevel = S7.GetBitAt(dbBuffer, 10, 2);
                BriqData.OilLess15 = S7.GetBitAt(dbBuffer, 10, 3);
                BriqData.OilMore75 = S7.GetBitAt(dbBuffer,10, 4);
                BriqData.PressureSensorError = S7.GetBitAt(dbBuffer, 10, 5);
                BriqData.TempSensorError = S7.GetBitAt(dbBuffer, 10, 6);
                BriqData.CylindersNotInHome = S7.GetBitAt(dbBuffer, 10, 7);
                BriqData.MainCylinderPressureError = S7.GetBitAt(dbBuffer, 11, 0);
                BriqData.VerticalCylinderPressureError = S7.GetBitAt(dbBuffer, 11, 1);
                BriqData.FormCylinderPressureError = S7.GetBitAt(dbBuffer, 11, 2);
                BriqData.FreeStrokeMainCylinder = S7.GetBitAt(dbBuffer, 11, 3);
                BriqData.FreeStrokeVerticalCylinder = S7.GetBitAt(dbBuffer, 11, 4);
                BriqData.FreeStrokeFormCylinder = S7.GetBitAt(dbBuffer, 11, 5);
                BriqData.NoSawdust = S7.GetBitAt(dbBuffer, 11, 6);
                BriqData.Lifebit = S7.GetBitAt(dbBuffer, 11, 7);

            } else
                AddLogError("Ошибка чтения с контроллера: " + BriqData.result);

            return BriqData;

        }

        private bool WriteToDB(string sDBTableName, BriquettesData BriqData)
        {
            int res = -1;
            BriquettesData dbData = new BriquettesData();

            try
            {
                //Считываем переменную из базы
                using (SqlCommand dbCommand = dbConn.CreateCommand())
                {
                    dbCommand.CommandText = "SELECT TOP 1 [Error], [PressOff], [ManualMode], [AutoMode], [Counter], [MotorHours], [Safety], [DirtyFilter], [OilLevel], [OilLess15], [OilMore75], [PressureSensorError], [TempSensorError], " +
                        "[CylindersNotInHome], [MainCylinderPressureError], [VerticalCylinderPressureError], [FormCylinderPressureError], [FreeStrokeMainCylinder], [FreeStrokeVerticalCylinder], [FreeStrokeFormCylinder], [NoSawdust], " +
                        "[Lifebit] FROM " + sDBTableName + " ORDER BY [id] DESC";
                    SqlDataReader reader = dbCommand.ExecuteReader();
                    while (reader.Read())
                    {
                        dbData.Error = Convert.ToBoolean(reader?["Error"]);
                        dbData.PressOff = Convert.ToBoolean(reader?["PressOff"]);
                        dbData.ManualMode = Convert.ToBoolean(reader?["ManualMode"]);
                        dbData.AutoMode = Convert.ToBoolean(reader?["AutoMode"]);
                        dbData.Counter = Convert.ToInt64(reader["Counter"]);
                        dbData.MotorHours = Convert.ToInt64(reader["MotorHours"]);
                        dbData.Safety = Convert.ToBoolean(reader?["Safety"]);
                        dbData.DirtyFilter = Convert.ToBoolean(reader?["DirtyFilter"]);
                        dbData.OilLevel = Convert.ToBoolean(reader?["OilLevel"]);
                        dbData.OilLess15 = Convert.ToBoolean(reader?["OilLess15"]);
                        dbData.OilMore75 = Convert.ToBoolean(reader?["OilMore75"]);
                        dbData.PressureSensorError = Convert.ToBoolean(reader?["PressureSensorError"]);
                        dbData.TempSensorError = Convert.ToBoolean(reader?["TempSensorError"]);
                        dbData.CylindersNotInHome = Convert.ToBoolean(reader?["CylindersNotInHome"]);
                        dbData.MainCylinderPressureError = Convert.ToBoolean(reader?["MainCylinderPressureError"]);
                        dbData.VerticalCylinderPressureError = Convert.ToBoolean(reader?["VerticalCylinderPressureError"]);
                        dbData.FormCylinderPressureError = Convert.ToBoolean(reader?["FormCylinderPressureError"]);
                        dbData.FreeStrokeMainCylinder = Convert.ToBoolean(reader?["FreeStrokeMainCylinder"]);
                        dbData.FreeStrokeVerticalCylinder = Convert.ToBoolean(reader?["FreeStrokeVerticalCylinder"]);
                        dbData.FreeStrokeFormCylinder = Convert.ToBoolean(reader?["FreeStrokeFormCylinder"]);
                        dbData.NoSawdust = Convert.ToBoolean(reader?["NoSawdust"]);
                        dbData.Lifebit = Convert.ToBoolean(reader?["Lifebit"]);
                    }
                    reader.Close();
                    dbData.result = 0;
                }
            }

            catch (Exception exdb)
            {
                AddLogError("Ошибка чтения БД: " + exdb.Message);
                dbData.result = -1;

            }
            if (dbData.Equals(BriqData))
                AddLog("Данные не изменились");
            else
            {

                try
                {
                    using (SqlCommand dbCommand = dbConn.CreateCommand())
                    {
                        dbCommand.CommandText = String.Format("INSERT INTO " + sDBTableName + " ([datetime], [Error], [PressOff], [ManualMode], [AutoMode], [Counter], [MotorHours], [Safety], [DirtyFilter], [OilLevel], [OilLess15], " +
                            "[OilMore75], [PressureSensorError], [TempSensorError], [CylindersNotInHome], [MainCylinderPressureError], [VerticalCylinderPressureError], [FormCylinderPressureError], [FreeStrokeMainCylinder], " +
                            "[FreeStrokeVerticalCylinder], [FreeStrokeFormCylinder], [NoSawdust], [Lifebit]) VALUES (@datetime, @Error, @PressOff, @ManualMode, @AutoMode, @Counter, @MotorHours, @Safety, @DirtyFilter, " +
                            "@OilLevel, @OilLess15, @OilMore75, @PressureSensorError, @TempSensorError, @CylindersNotInHome, @MainCylinderPressureError, @VerticalCylinderPressureError, @FormCylinderPressureError, @FreeStrokeMainCylinder, " +
                            "@FreeStrokeVerticalCylinder, @FreeStrokeFormCylinder, @NoSawdust, @Lifebit) ");


                        dbCommand.Parameters.Add("datetime", SqlDbType.DateTime).Value = CurrentDate;
                        dbCommand.Parameters.Add("Error", SqlDbType.Bit).Value = BriqData.Error;
                        dbCommand.Parameters.Add("PressOff", SqlDbType.Bit).Value = BriqData.PressOff;
                        dbCommand.Parameters.Add("ManualMode", SqlDbType.Bit).Value = BriqData.ManualMode;
                        dbCommand.Parameters.Add("AutoMode", SqlDbType.Bit).Value = BriqData.AutoMode;
                        dbCommand.Parameters.Add("Counter", SqlDbType.BigInt).Value = BriqData.Counter;
                        dbCommand.Parameters.Add("MotorHours", SqlDbType.BigInt).Value = BriqData.MotorHours;
                        dbCommand.Parameters.Add("Safety", SqlDbType.Bit).Value = BriqData.Safety;
                        dbCommand.Parameters.Add("DirtyFilter", SqlDbType.Bit).Value = BriqData.DirtyFilter;
                        dbCommand.Parameters.Add("OilLevel", SqlDbType.Bit).Value = BriqData.OilLevel;
                        dbCommand.Parameters.Add("OilLess15", SqlDbType.Bit).Value = BriqData.OilLess15;
                        dbCommand.Parameters.Add("OilMore75", SqlDbType.Bit).Value = BriqData.OilMore75;
                        dbCommand.Parameters.Add("PressureSensorError", SqlDbType.Bit).Value = BriqData.PressureSensorError;
                        dbCommand.Parameters.Add("TempSensorError", SqlDbType.Bit).Value = BriqData.TempSensorError;
                        dbCommand.Parameters.Add("CylindersNotInHome", SqlDbType.Bit).Value = BriqData.CylindersNotInHome;
                        dbCommand.Parameters.Add("MainCylinderPressureError", SqlDbType.Bit).Value = BriqData.MainCylinderPressureError;
                        dbCommand.Parameters.Add("VerticalCylinderPressureError", SqlDbType.Bit).Value = BriqData.VerticalCylinderPressureError;
                        dbCommand.Parameters.Add("FormCylinderPressureError", SqlDbType.Bit).Value = BriqData.FormCylinderPressureError;
                        dbCommand.Parameters.Add("FreeStrokeMainCylinder", SqlDbType.Bit).Value = BriqData.FreeStrokeMainCylinder;
                        dbCommand.Parameters.Add("FreeStrokeVerticalCylinder", SqlDbType.Bit).Value = BriqData.FreeStrokeVerticalCylinder;
                        dbCommand.Parameters.Add("FreeStrokeFormCylinder", SqlDbType.Bit).Value = BriqData.FreeStrokeFormCylinder;
                        dbCommand.Parameters.Add("NoSawdust", SqlDbType.Bit).Value = BriqData.NoSawdust;
                        dbCommand.Parameters.Add("Lifebit", SqlDbType.Bit).Value = BriqData.Lifebit;

                        res = dbCommand.ExecuteNonQuery();
                    }
                }

                catch (Exception exdb)
                {
                    AddLogError("Ошибка записи БД: " + exdb.Message);
                }

            }

            return res == 0;
        }

        public void AddLog(string log)
        {
            try
            {
                Console.WriteLine(log);
                eventLog.WriteEntry(log, EventLogEntryType.Information);
            }
            catch (Exception ex)
            {
                AddLogError("Невозможно записать в журнал событий " + ex.Message);
            }
        }

        public void AddLogError(string log)
        {
            try
            {
                Console.WriteLine(log);
                eventLog.WriteEntry(log, EventLogEntryType.Error);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Невозможно записать в журнал событий " + ex.Message);
            }
        }

        private bool PlcConnect(string Address, int Rack, int Slot)
        {
            int res = Client.ConnectTo(Address, Rack, Slot);
            if (res == 0)
                AddLog("  Подключено к контроллеру: " + Address + " (Rack=" + Rack.ToString() + ", Slot=" + Slot.ToString() + ")");
            else
            {
                if (res < 0)
                    AddLogError("Ошибка: Ошибка библиотеки (-1)\n");
                else
                    AddLogError("Ошибка подключения к контроллеру: " + Address + " (Rack=" + Rack.ToString() + ", Slot=" + Slot.ToString() + "): " + Client.ErrorText(res));
            }
            return res == 0;
        }

  

    }
}

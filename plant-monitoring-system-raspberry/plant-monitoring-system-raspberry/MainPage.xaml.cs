using Microsoft.IoT.Lightning.Providers;
using plant_monitoring_system_raspberry.Converters;
using plant_monitoring_system_raspberry.Devices.Gpio;
using plant_monitoring_system_raspberry.Devices.I2c.Ads1115;
using plant_monitoring_system_raspberry.Devices.I2c.Arduino;
using plant_monitoring_system_raspberry.Devices.I2c.Bmp180;
using plant_monitoring_system_raspberry.Devices.I2c.Si7021;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices;
using Windows.Graphics.Display;
using Windows.Media.Capture;
using Windows.System.Display;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using WinRTXamlToolkit.Controls.DataVisualization.Charting;

namespace plant_monitoring_system_raspberry
{
    /// <summary>
    /// Code-behind file. Contains the event handlers, and the application logic.
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        #region Fields
        // Fields of GPIO, I2c devices.
        private Ads1115Sensor adc1;
        private Ads1115Sensor adc2;
        private Arduino arduino;
        private Si7021Sensor temperatureHumiditySensor;
        private StepperMotor stepperMotor;
        private Bmp180Sensor temperaturePressureSensor;
        private GpioDevices gpioPins;

        // Web camera related fields.
        private MediaCapture capture;
        private DisplayRequest displayRequest = new DisplayRequest();
        private bool isPreviewing;

        private DispatcherTimer timer;
        private ObservableCollection<NameValueItem> dailyLuminosityValues = new ObservableCollection<NameValueItem>();
        private int ultrasonicDistance;
        private double thermistorTemperature;
        private readonly uint[] PLANT_POSITIONS = new uint[] { 270, 740, 1260, 1840 };
        private const int MAX_SAMPLES = 96;
        private readonly object lockObject = new object();
        #endregion

        #region Properties
        /// <value>
        /// Soil humidity of plant 1 as a raw ADC value.
        /// </value>
        public int Plant1SoilMoisture
        {
            get { return plant1SoilMoisture; }
            set { Set(ref plant1SoilMoisture, value); }
        }
        private int plant1SoilMoisture = 0;

        /// <value>
        /// Soil humidity of plant 2 as a raw ADC value.
        /// </value>
        public int Plant2SoilMoisture
        {
            get { return plant2SoilMoisture; }
            set { Set(ref plant2SoilMoisture, value); }
        }
        private int plant2SoilMoisture = 0;

        /// <value>
        /// Soil humidity of plant 3 as a raw ADC value.
        /// </value>
        public int Plant3SoilMoisture
        {
            get { return plant3SoilMoisture; }
            set { Set(ref plant3SoilMoisture, value); }
        }
        private int plant3SoilMoisture = 0;

        /// <value>
        /// Soil humidity of plant 4 as a raw ADC value.
        /// </value>
        public int Plant4SoilMoisture
        {
            get { return plant4SoilMoisture; }
            set { Set(ref plant4SoilMoisture, value); }
        }
        private int plant4SoilMoisture = 0;

        /// <value>
        /// Measured temperature in C°.
        /// </value>
        public double Temperature
        {
            get { return temperature; }
            set { Set(ref temperature, value); }
        }
        private double temperature = 0;

        /// <value>
        /// Measured humidity in %.
        /// </value>
        public double Humidity
        {
            get { return humidity; }
            set { Set(ref humidity, value); }
        }
        private double humidity = 0;

        /// <value>
        /// Measured pressure value in Pa.
        /// </value>
        public double Pressure
        {
            get { return pressure; }
            set { Set(ref pressure, value); }
        }
        private double pressure = 0;

        /// <value>
        /// A boolean value that represents whether automatic or manual mode is active.
        /// </value>
        public bool Mode
        {
            get { return mode; }
            set { Set(ref mode, value); }
        }
        private bool mode = true;
        #endregion

        /// <summary>
        /// Class constructor that starts the program.
        /// </summary>
        /// <remarks>
        /// The class constructor sets the DataContext, subscribe events 
        /// to the Loaded and Unloaded events, allocate LineSeries's 
        /// Source and initializes and starts the timer.
        /// </remarks>
        public MainPage()
        {
            this.InitializeComponent();

            // Setting the DataContext
            this.DataContext = this;

            // Register for the unloaded event so we can clean up upon exit
            Unloaded += MainPage_Unloaded;
            Loaded += MainPage_Loaded;

            // Set Lightning as the default provider
            if (LightningProvider.IsLightningEnabled)
                LowLevelDevicesController.DefaultProvider = LightningProvider.GetAggregateProvider();

            // Assign the ObservableCollection to the LineSeries
            (LineChart.Series[0] as LineSeries).ItemsSource = dailyLuminosityValues;

            // Initialize the DispatcherTimer
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10)
            };

            // Subscribe TimerTickAsync to the Tick event
            timer.Tick += TimerTickAsync;

            //Starting the timer
            timer.Start();
        }

        /// <summary>
        /// Creates and initializes the implemented hardvare controllers and executes 
        /// the first measurement and starts the camera preview.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                //create and initiaize the hardvare controller objects
                stepperMotor = new StepperMotor();
                await stepperMotor.InitializeAsync();

                adc1 = new Ads1115Sensor(AdcAddress.VCC);
                await adc1.InitializeAsync();

                adc2 = new Ads1115Sensor(AdcAddress.GND);
                await adc2.InitializeAsync();

                arduino = new Arduino();
                await arduino.InitializeAsync();

                gpioPins = new GpioDevices();
                await gpioPins.InitializeAsync();

                temperatureHumiditySensor = new Si7021Sensor();
                await temperatureHumiditySensor.InitializeAsync();

                temperaturePressureSensor = new Bmp180Sensor();
                await temperaturePressureSensor.InitializeAsync();

                // Execute the first measurement
                await ExecuteMeasurementAsync();

                // Start the camera preview
                await StartPreviewAsync();

                /*
                #if !DEBUG
                await StartPreviewAsync();
                #endif
                */
            }
            catch (Exception ex)
            {
                throw new Exception("Initialization has failed: " + ex);
            }
        }

        /// <summary>
        /// Free up the resources.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            // Calling Dispose on the hardware controller objects
            stepperMotor.Dispose(); // if (stepperMotor != null) { stepperMotor.Dispose(); stepperMotor = null;}
            adc1.Dispose();
            adc2.Dispose();
            arduino.Dispose();
            temperatureHumiditySensor.Dispose();
            temperaturePressureSensor.Dispose();
            gpioPins.Dispose();

            // Stops the timer and camera preview
            timer.Stop();
            timer = null;
            await StopPreviewAsync();

            /*
            #if !DEBUG
            await StartPreviewAsync();
            #endif
            */
        }

        /// <summary>
        /// Periodically executes a measurement and in automatic mode executes the irrigation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void TimerTickAsync(object sender, object e)
        {
            // Do a single measuremen
            await ExecuteMeasurementAsync();

            if (Mode)
            {

                if (ultrasonicDistance <= 0)
                {
                    TextBlockDebug.Text = "(!) Figyelem! Nincs elérhető információ a vízszintről.";
                }
                else if (ultrasonicDistance > 11)
                {
                    TextBlockDebug.Text = "(!) Hiba! Túl alacsony a vízszint az öntözéshez.";
                    return;
                }

                // Select the dryest plant
                var items = new int[] { Plant1SoilMoisture, Plant2SoilMoisture, Plant3SoilMoisture, Plant4SoilMoisture };

                int index = 0;
                int max = items[0];

                for (int i = 1; i < items.Length; ++i)
                {
                    if (items[i] > max)
                    {
                        max = items[i];
                        index = i;
                    }
                }

                // Check if the dryest plant needs irrigation
                if (SoilMoistureToColorConverter.ConvertValueToCategory(items[index]) > 2)
                {
                    RadioModeAutomatic.IsEnabled = false;
                    RadioModeManual.IsEnabled = false;

                    timer.Stop();
                    await Task.Delay(100);

                    // Irrigation process
                    await stepperMotor.CalibrateAsync();
                    await stepperMotor.MoveToPositionAsync(PLANT_POSITIONS[index]);
                    await gpioPins.StartIrrigationAsync(20000);
                    await Task.Delay(5000);
                    await stepperMotor.MoveToPositionAsync(0);

                    timer.Start();

                    RadioModeAutomatic.IsEnabled = true;
                    RadioModeManual.IsEnabled = true;
                }
            }
        }

        /// <summary>
        /// Executes a measurement session.
        /// </summary>
        /// <returns></returns>
        private async Task ExecuteMeasurementAsync()
        {
            // Measuring temperature and pressure
            if (temperaturePressureSensor != null && temperaturePressureSensor.IsInitialized)
            {
                try
                {
                    var reading = await temperaturePressureSensor.GetSensorDataAsync(Bmp180AccuracyMode.UltraHighResolution);
                    Temperature = reading.Temperature;
                    Pressure = reading.Pressure;
                }
                catch (Exception ex)
                {
                    throw new Exception("BMP180 temperature and pressure measurement has failed" + ex);
                }
            }

            // Measuring soil humidity values
            Ads1115SensorSetting setting1 = new Ads1115SensorSetting()
            {
                Pga = AdcPga.G1,
                Mode = AdcMode.SINGLESHOOT_CONVERSION
            };

            if (adc1 != null && adc1.IsInitialized)
            {
                try
                {
                    gpioPins.SoilSensorPowerOn();
                    await Task.Delay(2);

                    setting1.Input = AdcInput.A0_SE;
                    var reading = await adc1.ReadSingleShot(setting1);
                    Plant2SoilMoisture = reading.DecimalValue;

                    setting1.Input = AdcInput.A1_SE;
                    reading = await adc1.ReadSingleShot(setting1);
                    Plant4SoilMoisture = reading.DecimalValue;

                    setting1.Input = AdcInput.A2_SE;
                    reading = await adc1.ReadSingleShot(setting1);
                    Plant1SoilMoisture = reading.DecimalValue;

                    setting1.Input = AdcInput.A3_SE;
                    reading = await adc1.ReadSingleShot(setting1);
                    Plant3SoilMoisture = reading.DecimalValue;

                    gpioPins.SoilSensorPowerOff();
                }
                catch (Exception ex)
                {
                    throw new Exception("ADC1 read has failed" + ex);
                }
            }

            // Measuring luminosity
            Ads1115SensorSetting setting2 = new Ads1115SensorSetting()
            {
                Pga = AdcPga.G4, //(vagy G8)
                Mode = AdcMode.SINGLESHOOT_CONVERSION,
                Input = AdcInput.A0_SE
            };

            if (adc2 != null && adc2.IsInitialized)
            {
                try
                {
                    var reading = await adc1.ReadSingleShot(setting1);

                    if (dailyLuminosityValues.Count == MAX_SAMPLES)
                        dailyLuminosityValues.RemoveAt(0);
                    dailyLuminosityValues.Add(new NameValueItem()
                    {
                        Value = reading.DecimalValue
                    });
                }
                catch (Exception ex)
                {
                    throw new Exception("ADC2 read has failed" + ex);
                }
            }

            // Measuring ultrasonic distance
            if (arduino != null && arduino.IsInitialized)
            {
                try
                {
                    var distance = await arduino.ReadWaterLevelAsync();
                    if (distance != -1 && distance != -2)
                        ultrasonicDistance = distance;

                    thermistorTemperature = await arduino.ReadThermistorAsync();
                }
                catch (Exception ex)
                {
                    throw new Exception("Arduino measurement has failed" + ex);
                }
            }

            // Measuring humidity
            if (temperatureHumiditySensor != null && temperatureHumiditySensor.IsInitialized)
            {
                try
                {
                    double reading = await temperatureHumiditySensor.MeasureHumidityAsync(false, false);

                    if (reading < 0)
                        reading = 0;
                    else if (reading > 100)
                        reading = 100;

                    Humidity = reading;
                }
                catch (Exception ex)
                {
                    throw new Exception("SI7021 humidity measurement has failed" + ex);
                }
            }
        }

        #region CameraHandlerMethods
        /// <summary>
        /// Starts camera preview.
        /// </summary>
        /// <returns></returns>
        public async Task StartPreviewAsync()
        {
            try
            {
                if (isPreviewing)
                    return;

                // New instance of the MediaCapture class.
                capture = new MediaCapture();

                // Initialize the camera. It must be called from the main UI thread.
                await capture.InitializeAsync();

                //capture.Failed += MediaCaptureFailedEventHandler(captureFailed);

                // Connect the MediaCapture to the CaptureElement by setting the source property.
                PreviewElement.Source = capture;

                // Starting of the preview. Throws FileLoadException.
                await capture.StartPreviewAsync();

                // Stopping the device to go sleep mode and prevent rotating.
                displayRequest.RequestActive();
                DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;

                isPreviewing = true;
            }
            catch (UnauthorizedAccessException)
            {
                TextBlockDebug.Text += "\n Camera initialization failed! Access is disabled by the user or missing capabilities. Check app manifest!";
            }
            catch (FileLoadException)
            {
                TextBlockDebug.Text += "\n Camera initialization failed! Another app has exclusive acces to the camera";
            }
            catch (Exception ex)
            {
                TextBlockDebug.Text += "\n Unable to initialize the camera: " + ex.Message;
            }
        }

        /// <summary>
        /// Stops camera preview.
        /// </summary>
        /// <returns></returns>
        public async Task StopPreviewAsync()
        {
            if (capture != null)
            {
                if (isPreviewing)
                {
                    await capture.StopPreviewAsync();
                    isPreviewing = false;
                }

                // Execute on the UI thread.
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    PreviewElement.Source = null;
                    if (displayRequest != null)
                    {
                        // Allow the screen to turn off. 
                        displayRequest.RequestRelease();
                    }
                });

                capture.Dispose();
                capture = null;
            }
        }
        #endregion

        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Sets the new value and raises the PropertyChanged event.
        /// </summary>
        /// <remarks>
        /// Checks whether <paramref name="storage"/> parameter is equal than
        /// the new <paramref name="value"/> parameter. If not it replaces 
        /// with the new one and calls <see cref="RaisePropertyChanged(string)"/> method.
        /// </remarks> 
        /// <typeparam name="T"></typeparam>
        /// <param name="storage">Old value</param>
        /// <param name="value">New value</param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public bool Set<T>(ref T storage, T value, [CallerMemberName]string propertyName = null)
        {
            // if unchanged return false
            if (Equals(storage, value))
                return false;
            storage = value;
            RaisePropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Raises PropertyChanged event if it's not null.
        /// </summary>
        /// <param name="propertyName"></param>
        private void RaisePropertyChanged(string propertyName)
        {
            // if PropertyChanged not null call the Invoke method
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region EventHandlers
        /// <summary>
        /// Event handler for the irrigation button. It does the irrigation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButtonIrrigationStart_ClickAsync(object sender, RoutedEventArgs e)
        {
            // Disable controls and stop timer beforre irrigation
            RadioModeAutomatic.IsEnabled = false;
            RadioModeManual.IsEnabled = false;
            timer.Stop();

            var selectedPlant = ComboPlantSelector.SelectedIndex;
            var selectedInterval = SliderIrrigationTimeSelector.Value;

            // Irrigation process
            await stepperMotor.CalibrateAsync();
            await stepperMotor.MoveToPositionAsync(PLANT_POSITIONS[(uint)selectedPlant]);
            await gpioPins.StartIrrigationAsync((int)selectedInterval*1000);
            await Task.Delay(5000);
            await stepperMotor.MoveToPositionAsync(0);

            // Restore to the previous state 
            timer.Start();
            RadioModeAutomatic.IsEnabled = true;
            RadioModeManual.IsEnabled = true;
        }

        /// <summary>
        /// Event handler for the button with hamburger icon.
        /// Opens and closes the side pane.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SplitOpenerButton_Click(object sender, RoutedEventArgs e)
        {
            MenuSplitView.IsPaneOpen = (MenuSplitView.IsPaneOpen == false) ? true : false;
        }
        #endregion
    }

}

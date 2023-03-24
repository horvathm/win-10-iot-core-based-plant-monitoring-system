using System.Threading.Tasks;

namespace plant_monitoring_system_raspberry.Devices.Gpio
{
    /// <summary>
    /// Functions and methods required to control the stepper motor.
    /// </summary>
    interface IStepperMotor
    {
        /// <summary>
        /// Initializes the sensor before use.
        /// </summary>
        /// <returns></returns>
        Task InitializeAsync();

        /// <summary>
        /// Calibrates the stepper motor to the absolute null position.
        /// </summary>
        /// <returns></returns>
        Task CalibrateAsync();

        /// <summary>
        /// Moves the stepper motor to the desired angle given in the
        /// <paramref name="angle"/> parameter.
        /// </summary>
        /// <param name="angle">
        /// The angle to move.
        /// </param>
        /// <returns></returns>
        Task MoveToAngleAsync(double angle);

        /// <summary>
        /// Moves the motor to the desired steps from the '0' point
        /// </summary>
        /// <param name="targetPosition"></param>
        /// <returns></returns>
        Task MoveToPositionAsync(uint targetPosition);
    }
}
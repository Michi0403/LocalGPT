# Embedded firmware and wiring

LocalGPT can create reviewable ESP32 and Arduino firmware plans from a board description, pin layout, sensor roles and transport requirements.

Planning is transport-neutral. GPIO, ADC, PWM, I²C, SPI, UART, CAN, RS-485, physical 1-Wire and LocalGPT logical telemetry are capabilities rather than forced choices.

The generated artifacts remain separate from compiler, serial and flashing operations. Those later operations require workspace permission assessment and explicit approval.

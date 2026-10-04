using AdlpLib.Core;
using AdlpLib.Core.Modules;
using System;

namespace AdlpLib.Devices
{
    public class Configurator : Core.Core
    {
        /// <summary>
        /// Модуль конфигуратора
        /// </summary>
        public ConfiguratorBaseModule Config;

        /// <summary>
        /// Конструктор
        /// </summary>
        public Configurator(Net N, Guid Uuid) : this(N, Uuid, new ConfiguratorBaseModule())
        {

        }

        /// <summary>
        /// Конструктор
        /// </summary>
        public Configurator(Net N, Guid Uuid, ConfiguratorBaseModule ConfigModule) : base(N, Uuid, "CFG")
        {
            Config = ConfigModule;
            AddModule(ConfigModule);
        }

        protected override bool OnInit()
        {
            if (!base.OnInit())
                return false;

            SetInited(true);
            return true;
        }
    }
}
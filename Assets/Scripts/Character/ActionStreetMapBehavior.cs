﻿using System;
using ActionStreetMap.Explorer.Commands;
using Assets.Scripts.Console;
using Assets.Scripts.Console.Utils;
using Assets.Scripts.Demo;
using ActionStreetMap.Core;
using ActionStreetMap.Explorer;
using ActionStreetMap.Explorer.Bootstrappers;
using ActionStreetMap.Infrastructure.Bootstrap;
using ActionStreetMap.Infrastructure.Config;
using ActionStreetMap.Infrastructure.Dependencies;
using ActionStreetMap.Infrastructure.Diagnostic;
using ActionStreetMap.Infrastructure.IO;
using UnityEngine;
using Component = ActionStreetMap.Infrastructure.Dependencies.Component;

namespace Assets.Scripts.Character
{
    public class ActionStreetMapBehavior : MonoBehaviour
    {
        public float Delta = 10;

        private GameRunner _gameRunner;

        private DemoTileListener _messageListener;

        private ITrace _trace;

        private Vector3 _position = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        
        private DebugConsole _console;

        // Use this for initialization
        private void Start()
        {
            Initialize();
        }

        // Update is called once per frame
        private void Update()
        {
            if (_position != transform.position)
            {
                _gameRunner.OnMapPositionChanged(
                    new MapPoint(transform.position.x, transform.position.z, transform.position.y));
                _position = transform.position;
            }
        }

        #region Initialization

        private void Initialize()
        {
            // create and register DebugConsole inside Container
            var container = new Container();
            var messageBus = new MessageBus();
            var pathResolver = new WinPathResolver();
            InitializeConsole(container);
            try
            {
                var fileSystemService = new FileSystemService(pathResolver);
                container.RegisterInstance(typeof(IPathResolver), pathResolver);
                container.RegisterInstance(typeof (IFileSystemService), fileSystemService);
                container.RegisterInstance<IConfigSection>(new ConfigSection(@"Config/settings.json", fileSystemService));

                // actual boot service
                container.Register(Component.For<IBootstrapperService>().Use<BootstrapperService>());

                // boot plugins
                container.Register(Component.For<IBootstrapperPlugin>().Use<InfrastructureBootstrapper>().Named("infrastructure"));
                container.Register(Component.For<IBootstrapperPlugin>().Use<TileBootstrapper>().Named("tile"));
                container.Register(Component.For<IBootstrapperPlugin>().Use<SceneBootstrapper>().Named("scene"));
                container.Register(Component.For<IBootstrapperPlugin>().Use<DemoBootstrapper>().Named("demo"));

                container.RegisterInstance(_trace);

                // this class will listen messages about tile processing from ASM engine
                _messageListener = new DemoTileListener(messageBus, _trace);

                // interception
                //container.AllowProxy = true;
                //container.AutoGenerateProxy = true;
                //container.AddGlobalBehavior(new TraceBehavior(_trace));

                _gameRunner = new GameRunner(container, messageBus);
                _gameRunner.RunGame();
            }
            catch (Exception ex)
            {
                _console.LogMessage(new ConsoleMessage("Error running game:" + ex.ToString(), RecordType.Error, Color.red));
                throw;
            }
        }

        private void InitializeConsole(IContainer container)
        {
            var consoleGameObject = new GameObject("_DebugConsole_");
            _console = consoleGameObject.AddComponent<DebugConsole>();
            container.RegisterInstance(_console);
            // that is not nice, but we need to use commands registered in DI with their dependencies
            _console.Container = container; 
            _trace = new DebugConsoleTrace(_console);

            //_console._controller.Register("scene", new SceneCommand(container));
        }

        #endregion
    }
}

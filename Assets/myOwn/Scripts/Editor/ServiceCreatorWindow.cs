using System.IO;
using UnityEditor;
using UnityEngine;

namespace MyOwn.ServiceHarness.Editor
{
    /// <summary>
    /// Editor tool: sinh boilerplate cho service mới + payload struct + log lệnh register.
    /// Menu: Tools > Services > Create New Service.
    /// </summary>
    public sealed class ServiceCreatorWindow : EditorWindow
    {
        private string _serviceName = "MyNew";
        private LifetimeChoice _lifetime = LifetimeChoice.Singleton;
        private string _outputFolder = "Assets/myOwn/Scripts/Examples";

        private enum LifetimeChoice { Singleton, Scoped, Transient }

        [MenuItem("Tools/Services/Create New Service")]
        private static void ShowWindow()
        {
            var window = GetWindow<ServiceCreatorWindow>("Service Creator");
            window.minSize = new Vector2(400, 220);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("New Service", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _serviceName = EditorGUILayout.TextField("Service Name (no suffix)", _serviceName);
            _lifetime = (LifetimeChoice)EditorGUILayout.EnumPopup("Lifetime", _lifetime);
            _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Sẽ sinh:\n" +
                $"  • {_serviceName}Service.cs (POCO + IService + IAsyncStartable)\n" +
                $"  • {_serviceName}TickPayload.cs (readonly struct)\n" +
                "Sau khi tạo, Console sẽ log đoạn register code để bạn copy vào LifetimeScope.",
                MessageType.Info
            );

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_serviceName)))
            {
                if (GUILayout.Button("Create", GUILayout.Height(32)))
                {
                    CreateFiles();
                }
            }
        }

        private void CreateFiles()
        {
            if (!Directory.Exists(_outputFolder))
            {
                Directory.CreateDirectory(_outputFolder);
            }

            var className = _serviceName + "Service";
            var payloadName = _serviceName + "TickPayload";
            var servicePath = Path.Combine(_outputFolder, className + ".cs");
            var payloadPath = Path.Combine(_outputFolder, payloadName + ".cs");

            if (File.Exists(servicePath) || File.Exists(payloadPath))
            {
                EditorUtility.DisplayDialog("Service Creator",
                    $"File đã tồn tại:\n{servicePath}\n{payloadPath}\nXoá trước rồi thử lại.",
                    "OK");
                return;
            }

            File.WriteAllText(servicePath, BuildServiceContent(className, payloadName));
            File.WriteAllText(payloadPath, BuildPayloadContent(payloadName));
            AssetDatabase.Refresh();

            var registerSnippet =
                $"builder.Register<{className}>(Lifetime.{_lifetime})\n" +
                $"    .AsImplementedInterfaces()\n" +
                $"    .AsSelf();";

            Debug.Log(
                $"[ServiceCreator] Created:\n" +
                $"  • {servicePath}\n" +
                $"  • {payloadPath}\n\n" +
                $"📋 TODO: Paste vào RootLifetimeScope.Configure() (Singleton) " +
                $"hoặc GameLifetimeScope.Configure() (Scoped):\n\n{registerSnippet}"
            );

            EditorUtility.DisplayDialog("Service Creator",
                $"Đã tạo {className} + {payloadName}.\nXem Console để copy lệnh register.",
                "OK");
        }

        private static string BuildServiceContent(string className, string payloadName)
        {
            return
$@"using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;

namespace MyOwn.ServiceHarness
{{
    /// <summary>
    /// TODO: Mô tả service.
    /// </summary>
    public sealed class {className} : IService, IAsyncStartable
    {{
        private readonly IPublisher<{payloadName}> _publisher;

        public {className}(IPublisher<{payloadName}> publisher)
        {{
            _publisher = publisher;
        }}

        public UniTask StartAsync(CancellationToken cancellation)
        {{
            // TODO: init logic
            Debug.Log(""[{className}] StartAsync"");
            return UniTask.CompletedTask;
        }}

        public UniTask InitializeAsync(CancellationToken ct = default) => UniTask.CompletedTask;
    }}
}}
";
        }

        private static string BuildPayloadContent(string payloadName)
        {
            return
$@"namespace MyOwn.ServiceHarness
{{
    /// <summary>
    /// TODO: Mô tả payload.
    /// </summary>
    public readonly struct {payloadName}
    {{
        // TODO: thêm fields readonly
    }}
}}
";
        }
    }
}

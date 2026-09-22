using System;
using System.Threading;
using System.Globalization;
using System.Threading.Tasks;
using IPA.Logging;
using OculusStudios.Platform.Core;
using SiraUtil.Zenject;
using Zenject;

namespace HRCounter.Utils;

internal class UserInfoHelper : IAsyncInitializable
{
    private const int RETRY = 5;

    [Inject]
    private readonly Logger _logger = null!;

    [Inject]
    private readonly IPlatform _platform = null!;

    public UserInfo? UserInfo { get; private set; }

    Task IAsyncInitializable.InitializeAsync(CancellationToken token) => LoadUserInfo(token);

    private async Task LoadUserInfo(CancellationToken token)
    {
        for (var i = 0; i < RETRY; i++)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                if (i > 0)
                {
                    await Task.Delay((int)(1000 * Math.Pow(2, i)), token);
                }

                var user = _platform.user;
                if (user != null && user.userId != 0)
                {
                    var platform = _platform.vendor switch
                    {
                        Vendor.Valve => global::UserInfo.Platform.Steam,
                        Vendor.Meta => global::UserInfo.Platform.Oculus,
                        Vendor.Sony => global::UserInfo.Platform.PS5,
                        _ => global::UserInfo.Platform.Test
                    };
                    UserInfo = new UserInfo(platform, user.userId.ToString(CultureInfo.InvariantCulture), user.displayName);
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                // we exit
                _logger.Debug("LoadUserInfo cancelled");
                return;
            }
            catch (Exception e)
            {
                _logger.Warn($"Failed to load user info ({i + 1}/{RETRY}): {e.Message}");
                _logger.Warn(e);
            }
        }

        _logger.Warn("Failed to load user info after all retries");
    }
}

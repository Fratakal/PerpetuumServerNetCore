﻿using System;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Log;
using Perpetuum.Threading.Process;

namespace Perpetuum.Services.ExtensionService
{
    public class GiveExtensionPointsService : Process
    {
        private readonly int BASEPOINTS;
        private readonly int BONUSPOINTS;
        private readonly int SENDNUMBER;
        private readonly EPConfiguration _globalEpConfiguration;
        private bool _workInProgress;

        public GiveExtensionPointsService(GlobalConfiguration globalConfiguration)
        {
            var ep = _globalEpConfiguration = globalConfiguration.ExtentionPoints;
            var mult = ep.IsEpCycleUseMultiply ? ep.EPBonusPointMultiplier : 1;

            BASEPOINTS = ep.EPBasePointPerCycle * mult;
            BONUSPOINTS = globalConfiguration.ExtentionPoints.EPBasePointPerCycle * ep.EPBonusPointMultiplier * mult;
            SENDNUMBER = ep.EPDistributionPerDay - 1;
        }

        public override void Update(TimeSpan time)
        {
            GiveExtensionPointsToAccounts();
        }

        private void GiveExtensionPointsToAccounts()
        {
            var now = DateTime.Now;
            if (_globalEpConfiguration.EPCycleTimeEnable && (now.Hour < _globalEpConfiguration.EPCycleFromTime || now.Hour > _globalEpConfiguration.EPCycleToTime))
                return;

            if (_workInProgress)
            {
                Logger.Info("extension points working currently, skipping");
                return;
            }

            try
            {
                _workInProgress = true;

                var elapsed = Profiler.ExecutionTimeOf(() =>
                {
                    //was it done today?
                    if (WasExtensionPointsCheckToday(now))
                        return;
                    var sqlTime = DoGiveExtensionPointsToAccounts();
                    InformAffectedCharacters(sqlTime);
                });

                Logger.Info(elapsed.TotalMilliseconds + " ms was used to give extension points");
            }
            finally
            {
                _workInProgress = false;
            }
        }

        public void InformAffectedCharacters(DateTime now)
        {
            foreach (var grp in
                Db.Query().CommandText(@"SELECT distinct  c.characterID,e.points FROM dbo.extensionpoints e JOIN characters c ON e.accountid = c.accountID
WHERE (e.points=@basePoints OR e.points=@bonusPoints)
AND c.inUse=1 
AND e.eventtime >= @now
")
                    .SetParameter("@now", now)
                    .SetParameter("@basePoints",BASEPOINTS )
                    .SetParameter("@bonusPoints",BONUSPOINTS)
                    .Execute()
                    .GroupBy(r => r.GetValue<int>(1))
                )
            {
                if (grp.Key == BASEPOINTS)
                {
                    //leecher accounts
                    var affectedLeechers = grp.Select(r => Character.Get(r.GetValue<int>(0))).Distinct().ToArray();
                    Logger.Info($"Daily Extension Point Add: {affectedLeechers.Length} characters will be informed with point {BASEPOINTS} - leechers.");
                    ExtensionHelper.CreateExtensionPointsIncreasedMessage(BASEPOINTS).ToCharacters(affectedLeechers).Send();
                }
                else
                {
                    //paying customers
                    var affectedPayingCustomers = grp.Select(r => Character.Get(r.GetValue<int>(0))).Distinct().ToArray();
                    Logger.Info($"Daily Extension Point Add: {affectedPayingCustomers.Length} characters will be informed with point {BONUSPOINTS} - good guys.");
                    ExtensionHelper.CreateExtensionPointsIncreasedMessage(BONUSPOINTS).ToCharacters(affectedPayingCustomers).Send();
                }
            }
        }

        public DateTime DoGiveExtensionPointsToAccounts()
        {
            var sqlTime =
            Db.Query().CommandText("extensionPointsAdd")
                    .SetParameter("@basePoints", BASEPOINTS)
                    .SetParameter("@bonusPoints", BONUSPOINTS)
                    .ExecuteScalar<DateTime>();


            return sqlTime;
        }

        private bool WasExtensionPointsCheckToday(DateTime dayAgainst)
        {
            var count =
                Db.Query().CommandText("extensionPointsCheck")
                        .SetParameter("@now", dayAgainst)
                        .ExecuteScalar<int>();

#if DEBUG
            Logger.Info(count > 0 ? "extension points were already added today" : "no extension points were added today");
#endif
            return count > SENDNUMBER;
        }
    }
}

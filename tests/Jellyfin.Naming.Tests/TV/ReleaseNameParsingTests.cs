using Emby.Naming.Common;
using Emby.Naming.TV;
using Xunit;

namespace Jellyfin.Naming.Tests.TV
{
    /// <summary>
    /// Regression tests for scene-release folder/file names found in the "everything in one folder"
    /// torrent layout (movies and TV shows mixed in a single directory).
    /// </summary>
    public class ReleaseNameParsingTests
    {
        private static readonly NamingOptions _namingOptions = new();

        [Theory]
        // Show episode files must parse as episodes.
        [InlineData("/media/Andor.S02/Andor.S02E01.1080p.DSNP.WEB-DL.DDP5.1.Atmos.DV.HDR.H265.HuN.EnG-B9R.mkv", 2, 1)]
        [InlineData("/media/Silo.S03/Silo.S03E01.Who.Are.You.1080p.ATVP.WEB-DL.DDP5.1.Atmos.H.264-playWEB.mkv", 3, 1)]
        [InlineData("/media/Alien.Earth.S01/Alien.Earth.S01E08.1080p.DSNP.WEB-DL.DDP5.1.H.264.HUN.ENG-PTHD.mkv", 1, 8)]
        [InlineData("/media/Invincible.2021.S03/Invincible.2021.S03E02.1080p.AMZN.WEB-DL.DD+5.1.H.264-playWEB.mkv", 3, 2)]
        public void EpisodeResolver_ParsesRealEpisodeFiles(string path, int season, int episode)
        {
            var resolver = new EpisodeResolver(_namingOptions);
            var result = resolver.Resolve(path, false, true, false);

            Assert.NotNull(result);
            Assert.Equal(season, result.SeasonNumber);
            Assert.Equal(episode, result.EpisodeNumber);
        }

        [Theory]
        // Movie files must NOT parse as episodes.
        [InlineData("/media/28.Years.Later.2025/fulcrum-28.years.later.2025.1080p.webrip.ma.mkv")]
        [InlineData("/media/American Psycho 2000/American Psycho 2000 1080p CEE Blu-Ray ReMuX AVC DTS-HD 5.1-HiDeFZeN.mkv")]
        [InlineData("/media/1000.Men.And.Me/1000.Men.And.Me.The.Bonnie.Blue.Story.2025.HUN.WEB-DL.1080p.H.264-LEGION.mkv")]
        [InlineData("/media/A.Showder.Klub/A.Showder.Klub.bemutatja.40.20.Kiss.Adam.1080p.RTLP.WEB-DL.AAC2.0.H.264.HUN.mkv")]
        public void EpisodeResolver_DoesNotParseMovieFiles(string path)
        {
            var resolver = new EpisodeResolver(_namingOptions);
            var result = resolver.Resolve(path, false, true, false);

            Assert.Null(result);
        }

        [Theory]
        // Movie folders must NOT be detected as season folders.
        // These use the same flags as SeriesResolver.IsSeriesFolder -> IsSeasonFolder.
        [InlineData("1000.Men.And.Me.The.Bonnie.Blue.Story.2025.HUN.WEB-DL.1080p.H.264-LEGION")]
        [InlineData("28.Years.Later.2025.1080p.MA.WEBRip.DDP5.1.Atmos.x264.HUN-FULCRUM")]
        [InlineData("American Psycho 2000 1080p CEE Blu-Ray ReMuX AVC DTS-HD 5.1-HiDeFZeN")]
        [InlineData("A.Showder.Klub.bemutatja.40.20.Kiss.Adam.1080p.RTLP.WEB-DL.AAC2.0.H.264.HUN")]
        public void SeasonPathParser_MovieFolderIsNotASeason(string folderName)
        {
            var path = "/media/" + folderName;
            var result = SeasonPathParser.Parse(path, "/media", false, false);

            Assert.False(result.Success, $"Expected '{folderName}' not to be a season but got season {result.SeasonNumber}");
        }

        [Theory]
        // Real season folders must still be detected.
        [InlineData("/media/Show/Season 1", "/media/Show", 1)]
        [InlineData("/media/Show/S01", "/media/Show", 1)]
        [InlineData("/media/Show/Season 2", "/media/Show", 2)]
        public void SeasonPathParser_RealSeasonFolderIsASeason(string path, string parentPath, int season)
        {
            var result = SeasonPathParser.Parse(path, parentPath, false, false);

            Assert.True(result.Success);
            Assert.Equal(season, result.SeasonNumber);
            Assert.True(result.IsSeasonFolder);
        }

        [Theory]
        // Series name extraction from real show folders.
        [InlineData("Andor.S02.1080p.DSNP.WEB-DL.DDP5.1.Atmos.DV.HDR.H265.HuN.EnG-B9R", "Andor")]
        [InlineData("Alien.Earth.S01.1080p.DSNP.WEB-DL.DDP5.1.H.264.HUN.ENG-PTHD", "Alien Earth")]
        [InlineData("Silo.S03.1080p.ATVP.WEB-DL.DDP5.1.Atmos.H.264-playWEB", "Silo")]
        public void SeriesResolver_ExtractsNameFromReleaseFolder(string folderName, string expectedName)
        {
            var result = SeriesResolver.Resolve(_namingOptions, "/media/" + folderName);

            Assert.Equal(expectedName, result.Name);
        }
    }
}

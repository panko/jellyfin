using System;
using System.Collections.Generic;
using System.Linq;
using Emby.Naming.Common;
using Emby.Naming.Video;
using Emby.Server.Implementations.Library.Resolvers.Movies;
using Emby.Server.Implementations.Library.Resolvers.TV;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library
{
    /// <summary>
    /// Resolver tests for a "Mixed Movies &amp; TV Shows" library that contains both movie
    /// and TV-show release folders in a single directory (torrent "complete" style layout).
    /// </summary>
    public class MixedLibraryResolverTests
    {
        private static readonly NamingOptions _namingOptions = new();
        private static readonly VideoListResolver _videoListResolver = new(_namingOptions);

        // ---------------------------------------------------------------------------
        // SeriesResolver
        // ---------------------------------------------------------------------------

        [Fact]
        public void SeriesResolver_ShowFolder_ResolvesToSeries()
        {
            var resolver = new SeriesResolver(Mock.Of<ILogger<SeriesResolver>>(), _namingOptions);
            var args = ArgsForDirectory(
                "/media/Andor.S02.1080p.DSNP.WEB-DL.DDP5.1.Atmos.DV.HDR.H265.HuN.EnG-B9R",
                new Folder(),
                CollectionType.mixed,
                new FileSystemMetadata
                {
                    FullName = "/media/Andor.S02.1080p.DSNP.WEB-DL.DDP5.1.Atmos.DV.HDR.H265.HuN.EnG-B9R/Andor.S02E01.1080p.DSNP.WEB-DL.DDP5.1.Atmos.DV.HDR.H265.HuN.EnG-B9R.mkv",
                    Name = "Andor.S02E01.1080p.DSNP.WEB-DL.DDP5.1.Atmos.DV.HDR.H265.HuN.EnG-B9R.mkv"
                },
                new FileSystemMetadata
                {
                    FullName = "/media/Andor.S02.1080p.DSNP.WEB-DL.DDP5.1.Atmos.DV.HDR.H265.HuN.EnG-B9R/Andor.S02E02.1080p.DSNP.WEB-DL.DDP5.1.Atmos.DV.HDR.H265.HuN.EnG-B9R.mkv",
                    Name = "Andor.S02E02.1080p.DSNP.WEB-DL.DDP5.1.Atmos.DV.HDR.H265.HuN.EnG-B9R.mkv"
                });

            var result = resolver.ResolvePath(args);

            Assert.IsType<Series>(result);
        }

        [Theory]
        [InlineData("/media/28.Years.Later.2025.1080p.MA.WEBRip.DDP5.1.Atmos.x264.HUN-FULCRUM", "fulcrum-28.years.later.2025.1080p.webrip.ma.mkv")]
        [InlineData("/media/1000.Men.And.Me.The.Bonnie.Blue.Story.2025.HUN.WEB-DL.1080p.H.264-LEGION", "1000.Men.And.Me.The.Bonnie.Blue.Story.2025.HUN.WEB-DL.1080p.H.264-LEGION.mkv")]
        [InlineData("/media/American Psycho 2000 1080p CEE Blu-Ray ReMuX AVC DTS-HD 5.1-HiDeFZeN", "American Psycho 2000 1080p CEE Blu-Ray ReMuX AVC DTS-HD 5.1-HiDeFZeN.mkv")]
        public void SeriesResolver_MovieFolder_DoesNotResolveToSeries(string folderPath, string fileName)
        {
            var resolver = new SeriesResolver(Mock.Of<ILogger<SeriesResolver>>(), _namingOptions);
            var args = ArgsForDirectory(
                folderPath,
                new Folder(),
                CollectionType.mixed,
                new FileSystemMetadata
                {
                    FullName = folderPath + "/" + fileName,
                    Name = fileName
                });

            var result = resolver.ResolvePath(args);

            Assert.Null(result);
        }

        [Fact]
        public void SeriesResolver_LibraryRootWithNumberedMovieFolders_DoesNotResolveAsSeries()
        {
            // A folder that contains movie release folders (some of which start with a number)
            // must not be detected as a TV series. Regression guard for the
            // "folder starting with a number is misidentified as a season" bug.
            var resolver = new SeriesResolver(Mock.Of<ILogger<SeriesResolver>>(), _namingOptions);
            var args = ArgsForDirectory(
                "/media",
                new Folder(),
                CollectionType.mixed,
                new FileSystemMetadata { FullName = "/media/1000.Men.And.Me.The.Bonnie.Blue.Story.2025.HUN.WEB-DL.1080p.H.264-LEGION", IsDirectory = true },
                new FileSystemMetadata { FullName = "/media/28.Years.Later.2025.1080p.MA.WEBRip.DDP5.1.Atmos.x264.HUN-FULCRUM", IsDirectory = true },
                new FileSystemMetadata { FullName = "/media/American Psycho 2000 1080p CEE Blu-Ray ReMuX AVC DTS-HD 5.1-HiDeFZeN", IsDirectory = true });

            var result = resolver.ResolvePath(args);

            Assert.Null(result);
        }

        // ---------------------------------------------------------------------------
        // EpisodeResolver
        // ---------------------------------------------------------------------------

        [Fact]
        public void EpisodeResolver_FlatEpisodeFileInMixedLibrary_ResolvesToEpisode()
        {
            var resolver = new EpisodeResolver(Mock.Of<ILogger<EpisodeResolver>>(), _namingOptions, Mock.Of<IDirectoryService>());
            var args = new ItemResolveArgs(
                Mock.Of<IServerApplicationPaths>(),
                null)
            {
                Parent = new Folder(),
                CollectionType = CollectionType.mixed,
                FileInfo = new FileSystemMetadata
                {
                    FullName = "/media/Andor.S02E01.1080p.DSNP.WEB-DL.DDP5.1.Atmos.DV.HDR.H265.HuN.EnG-B9R.mkv"
                }
            };

            var result = resolver.ResolvePath(args);

            Assert.IsType<Episode>(result);
        }

        [Fact]
        public void EpisodeResolver_MovieFileInMixedLibrary_DoesNotResolveToEpisode()
        {
            var resolver = new EpisodeResolver(Mock.Of<ILogger<EpisodeResolver>>(), _namingOptions, Mock.Of<IDirectoryService>());
            var args = new ItemResolveArgs(
                Mock.Of<IServerApplicationPaths>(),
                null)
            {
                Parent = new Folder(),
                CollectionType = CollectionType.mixed,
                FileInfo = new FileSystemMetadata
                {
                    FullName = "/media/fulcrum-28.years.later.2025.1080p.webrip.ma.mkv"
                }
            };

            var result = resolver.ResolvePath(args);

            Assert.Null(result);
        }

        // ---------------------------------------------------------------------------
        // MovieResolver
        // ---------------------------------------------------------------------------

        [Fact]
        public void MovieResolver_FlatMovieFileInMixedLibrary_ResolvesToMovie()
        {
            var resolver = new MovieResolver(Mock.Of<IImageProcessor>(), Mock.Of<ILogger<MovieResolver>>(), _namingOptions, Mock.Of<IDirectoryService>(), _videoListResolver);
            var args = new ItemResolveArgs(
                Mock.Of<IServerApplicationPaths>(),
                null)
            {
                Parent = new Folder(),
                CollectionType = CollectionType.mixed,
                FileInfo = new FileSystemMetadata
                {
                    FullName = "/media/fulcrum-28.years.later.2025.1080p.webrip.ma.mkv"
                }
            };

            var result = resolver.ResolvePath(args);

            Assert.IsType<Movie>(result);
        }

        [Fact]
        public void MovieResolver_MovieFolderInMixedLibrary_ResolvesToMovie()
        {
            var libraryManager = new Mock<ILibraryManager>();
            libraryManager.Setup(m => m.GetLibraryOptions(It.IsAny<BaseItem>())).Returns(new LibraryOptions());
            libraryManager.Setup(m => m.IgnoreFile(It.IsAny<FileSystemMetadata>(), It.IsAny<BaseItem>())).Returns(false);

            var resolver = new MovieResolver(Mock.Of<IImageProcessor>(), Mock.Of<ILogger<MovieResolver>>(), _namingOptions, Mock.Of<IDirectoryService>(), _videoListResolver);
            var args = new ItemResolveArgs(
                Mock.Of<IServerApplicationPaths>(),
                libraryManager.Object)
            {
                Parent = new Folder(),
                CollectionType = CollectionType.mixed,
                FileInfo = new FileSystemMetadata
                {
                    FullName = "/media/28.Years.Later.2025.1080p.MA.WEBRip.DDP5.1.Atmos.x264.HUN-FULCRUM",
                    IsDirectory = true
                },
                FileSystemChildren = new[]
                {
                    new FileSystemMetadata
                    {
                        FullName = "/media/28.Years.Later.2025.1080p.MA.WEBRip.DDP5.1.Atmos.x264.HUN-FULCRUM/fulcrum-28.years.later.2025.1080p.webrip.ma.mkv",
                        Name = "fulcrum-28.years.later.2025.1080p.webrip.ma.mkv"
                    }
                }
            };

            var result = resolver.ResolvePath(args);

            Assert.IsType<Movie>(result);
        }

        [Fact]
        public void MovieResolver_MovieFolderWithSampleSubfolder_ResolvesToMovie()
        {
            var libraryManager = new Mock<ILibraryManager>();
            libraryManager.Setup(m => m.GetLibraryOptions(It.IsAny<BaseItem>())).Returns(new LibraryOptions());
            libraryManager.Setup(m => m.IgnoreFile(It.IsAny<FileSystemMetadata>(), It.IsAny<BaseItem>())).Returns(false);

            var resolver = new MovieResolver(Mock.Of<IImageProcessor>(), Mock.Of<ILogger<MovieResolver>>(), _namingOptions, Mock.Of<IDirectoryService>(), _videoListResolver);
            var args = new ItemResolveArgs(
                Mock.Of<IServerApplicationPaths>(),
                libraryManager.Object)
            {
                Parent = new Folder(),
                CollectionType = CollectionType.mixed,
                FileInfo = new FileSystemMetadata
                {
                    FullName = "/media/28.Years.Later.2025.1080p.MA.WEBRip.DDP5.1.Atmos.x264.HUN-FULCRUM",
                    IsDirectory = true
                },
                FileSystemChildren = new[]
                {
                    new FileSystemMetadata
                    {
                        FullName = "/media/28.Years.Later.2025.1080p.MA.WEBRip.DDP5.1.Atmos.x264.HUN-FULCRUM/fulcrum-28.years.later.2025.1080p.webrip.ma.mkv",
                        Name = "fulcrum-28.years.later.2025.1080p.webrip.ma.mkv"
                    },
                    new FileSystemMetadata
                    {
                        FullName = "/media/28.Years.Later.2025.1080p.MA.WEBRip.DDP5.1.Atmos.x264.HUN-FULCRUM/Sample",
                        Name = "Sample",
                        IsDirectory = true
                    }
                }
            };

            var result = resolver.ResolvePath(args);

            Assert.IsType<Movie>(result);
        }

        private static ItemResolveArgs ArgsForDirectory(string path, Folder parent, CollectionType? collectionType, params FileSystemMetadata[] children)
        {
            return new ItemResolveArgs(
                Mock.Of<IServerApplicationPaths>(),
                null)
            {
                Parent = parent,
                CollectionType = collectionType,
                FileInfo = new FileSystemMetadata
                {
                    FullName = path,
                    IsDirectory = true
                },
                FileSystemChildren = children
            };
        }

        [Fact]
        public void MovieResolver_ResolveMultiple_EpisodeFilesInMixedLibrary_AreNotClaimedAsMovies()
        {
            var resolver = new MovieResolver(Mock.Of<IImageProcessor>(), Mock.Of<ILogger<MovieResolver>>(), _namingOptions, Mock.Of<IDirectoryService>(), _videoListResolver);
            var parent = new Series { Name = "Andor" };
            var files = new List<FileSystemMetadata>
            {
                new FileSystemMetadata { FullName = "/media/Andor.S02/Andor.S02E01.1080p.mkv", Name = "Andor.S02E01.1080p.mkv" },
                new FileSystemMetadata { FullName = "/media/Andor.S02/Andor.S02E02.1080p.mkv", Name = "Andor.S02E02.1080p.mkv" }
            };

            var result = resolver.ResolveMultiple(parent, files, CollectionType.mixed, Mock.Of<IDirectoryService>());

            Assert.True(result is null || result.Items.Count == 0, "Episode files must not be batch-resolved as movies in a mixed library");
        }

        [Fact]
        public void MovieResolver_ResolveMultiple_MovieFilesInMixedLibrary_AreClaimedAsMovies()
        {
            var resolver = new MovieResolver(Mock.Of<IImageProcessor>(), Mock.Of<ILogger<MovieResolver>>(), _namingOptions, Mock.Of<IDirectoryService>(), _videoListResolver);
            var parent = new Folder();
            var files = new List<FileSystemMetadata>
            {
                new FileSystemMetadata { FullName = "/media/fulcrum-28.years.later.2025.1080p.webrip.ma.mkv", Name = "fulcrum-28.years.later.2025.1080p.webrip.ma.mkv" },
                new FileSystemMetadata { FullName = "/media/Andor.S02E01.1080p.mkv", Name = "Andor.S02E01.1080p.mkv" }
            };

            var result = resolver.ResolveMultiple(parent, files, CollectionType.mixed, Mock.Of<IDirectoryService>());

            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.IsType<Movie>(result.Items[0]);
            Assert.Contains(result.ExtraFiles, f => string.Equals(f.FullName, "/media/Andor.S02E01.1080p.mkv", StringComparison.Ordinal));
        }
    }
}

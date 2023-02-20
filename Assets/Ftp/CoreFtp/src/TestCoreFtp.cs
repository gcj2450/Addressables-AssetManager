using CoreFtp.Enum;
using CoreFtp;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.IO;
using CoreFtp.Infrastructure;
using System.Linq;
using UnityEngine.Assertions;
using System;
using System.Collections.ObjectModel;

public class TestCoreFtp : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        //Task t = Should_give_directory_listing();
    }

    public async Task Should_give_directory_listing()
    {
        using (var sut = new FtpClient(new FtpClientConfiguration
        {
            Host = "ftp://10.27.209.219:8021/",
            Username = "tmbh",
            Password = "anquan@123",
            Port = 8021,
            EncryptionType = FtpEncryption.None,
            IgnoreCertificateErrors = true,
            BaseDirectory = "/zionsdk-demo/"
        }))
        {
            await sut.LoginAsync();
            var directoryListing = await sut.ListAllAsync();
            Debug.Log("directoryListing.Count: " + directoryListing.Count);
            //directoryListing.Count.Should().BeGreaterThan(0);
        }
    }

    public async Task Should_fail_when_changing_to_a_nonexistent_directory(FtpEncryption encryption)
    {
        using (var sut = new FtpClient(new FtpClientConfiguration
        {
            Host = "ftp.bom.gov.au",
            Username = "anonymous",
            Password = "guest",
            Port = 21,
            EncryptionType = encryption,
            IgnoreCertificateErrors = true
        }))
        {
            await sut.LoginAsync();
            await sut.SetClientName(nameof(Should_fail_when_changing_to_a_nonexistent_directory));
            //await Assert.ThrowsAsync<FtpException>(() => sut.ChangeWorkingDirectoryAsync(Guid.NewGuid().ToString()));
        }
    }

    public async Task Should_change_to_directory_when_exists(FtpEncryption encryption)
    {
        string randomDirectoryName = Guid.NewGuid().ToString();

        using (var sut = new FtpClient(new FtpClientConfiguration
        {
            Host = "ftp.bom.gov.au",
            Username = "anonymous",
            Password = "guest",
            Port = 21,
            EncryptionType = encryption,
            IgnoreCertificateErrors = true
        }))
        {
            await sut.LoginAsync();
            await sut.CreateDirectoryAsync(randomDirectoryName);
            await sut.ChangeWorkingDirectoryAsync(randomDirectoryName);
            //sut.WorkingDirectory.Should().Be($"/{randomDirectoryName}");

            await sut.ChangeWorkingDirectoryAsync("../");
            await sut.DeleteDirectoryAsync(randomDirectoryName);
        }
    }

    public async Task Should_change_to_deep_directory_when_exists(FtpEncryption encryption)
    {
        string[] randomDirectoryNames =
        {
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString()
            };
        using (var sut = new FtpClient(new FtpClientConfiguration
        {
            Host = "ftp.bom.gov.au",
            Username = "anonymous",
            Password = "guest",
            Port = 21,
            EncryptionType = encryption,
            IgnoreCertificateErrors = true
        }))
        {
            string joinedPath = string.Join("/", randomDirectoryNames);
            await sut.LoginAsync();

            await sut.CreateDirectoryAsync(joinedPath);
            await sut.ChangeWorkingDirectoryAsync(joinedPath);
            //sut.WorkingDirectory.Should().Be($"/{joinedPath}");

            foreach (string directory in randomDirectoryNames.Reverse())
            {
                await sut.ChangeWorkingDirectoryAsync("../");
                await sut.DeleteDirectoryAsync(directory);
            }
        }
    }

    public async Task Should_create_directory_structure_recursively(FtpEncryption encryption)
    {
        using (var sut = new FtpClient(new FtpClientConfiguration
        {
            Host = "ftp.bom.gov.au",
            Username = "anonymous",
            Password = "guest",
            Port = 21,
            EncryptionType = encryption,
            IgnoreCertificateErrors = true
        }))
        {

            var guid = Guid.NewGuid().ToString();
            await sut.LoginAsync();
            await sut.SetClientName(nameof(Should_create_directory_structure_recursively));

            await sut.CreateDirectoryAsync($"{guid}/abc/123");

            //(await sut.ListDirectoriesAsync()).ToList()
            //                                    .Any(x => x.Name == guid)
            //                                    .Should().BeTrue();

            //await sut.ChangeWorkingDirectoryAsync(guid);

            //(await sut.ListDirectoriesAsync()).ToList()
            //                                    .Any(x => x.Name == "abc")
            //                                    .Should().BeTrue();
            await sut.ChangeWorkingDirectoryAsync("/");

            await sut.DeleteDirectoryAsync($"/{guid}/abc/123");
            await sut.DeleteDirectoryAsync($"/{guid}/abc");
            await sut.DeleteDirectoryAsync(guid);
        }
    }

    public async Task Should_create_a_directory(FtpEncryption encryption)
    {
        string randomDirectoryName = Guid.NewGuid().ToString();
        ReadOnlyCollection<FtpNodeInformation> directories;

        using (var sut = new FtpClient(new FtpClientConfiguration
        {
            Host = "ftp.bom.gov.au",
            Username = "anonymous",
            Password = "guest",
            Port = 21,
            EncryptionType = encryption,
            IgnoreCertificateErrors = true
        }))
        {
            await sut.LoginAsync();
            await sut.CreateDirectoryAsync(randomDirectoryName);
            directories = await sut.ListDirectoriesAsync();
            await sut.DeleteDirectoryAsync(randomDirectoryName);
            await sut.LogOutAsync();
        }

        //directories.Any(x => x.Name == randomDirectoryName).Should().BeTrue();
    }

    public async Task Should_throw_exception_when_folder_nonexistent(FtpEncryption encryption)
    {
        using (var sut = new FtpClient(new FtpClientConfiguration
        {
            Host = "ftp.bom.gov.au",
            Username = "anonymous",
            Password = "guest",
            Port = 21,
            EncryptionType = encryption,
            IgnoreCertificateErrors = true
        }))
        {
            string randomDirectoryName = Guid.NewGuid().ToString();
            await sut.LoginAsync();
            //await Assert.ThrowsAsync<FtpException>(() => sut.DeleteDirectoryAsync(randomDirectoryName));
            await sut.LogOutAsync();
        }
    }

    public async Task Should_delete_directory_when_exists(FtpEncryption encryption)
    {
        string randomDirectoryName = Guid.NewGuid().ToString();

        using (var sut = new FtpClient(new FtpClientConfiguration
        {
            Host = "ftp.bom.gov.au",
            Username = "anonymous",
            Password = "guest",
            Port = 21,
            EncryptionType = encryption,
            IgnoreCertificateErrors = true
        }))
        {
            await sut.LoginAsync();
            await sut.CreateDirectoryAsync(randomDirectoryName);
            //(await sut.ListDirectoriesAsync()).Any(x => x.Name == randomDirectoryName).Should().BeTrue();
            await sut.DeleteDirectoryAsync(randomDirectoryName);
            //(await sut.ListDirectoriesAsync()).Any(x => x.Name == randomDirectoryName).Should().BeFalse();
        }
    }

    public async Task Should_recursively_delete_folder(FtpEncryption encryption)
    {
        string randomDirectoryName = Guid.NewGuid().ToString();

        using (var sut = new FtpClient(new FtpClientConfiguration
        {
            Host = "ftp.bom.gov.au",
            Username = "anonymous",
            Password = "guest",
            Port = 21,
            EncryptionType = encryption,
            IgnoreCertificateErrors = true
        }))
        {
            await sut.LoginAsync();

            //await sut.CreateTestResourceWithNameAsync("penguin.jpg", $"{randomDirectoryName}/1/penguin.jpg");

            await sut.CreateDirectoryAsync($"{randomDirectoryName}/1/1/1");
            await sut.CreateDirectoryAsync($"{randomDirectoryName}/1/1/2");
            await sut.CreateDirectoryAsync($"{randomDirectoryName}/1/2/2");
            await sut.CreateDirectoryAsync($"{randomDirectoryName}/2/2/2");

            //(await sut.ListDirectoriesAsync()).Any(x => x.Name == randomDirectoryName).Should().BeTrue();
            //try
            //{
            //    await sut.DeleteDirectoryAsync(randomDirectoryName);
            //}
            //catch (Exception e)
            //{
            //    throw new Exception(e.Message.ToString());
            //}

            //(await sut.ListDirectoriesAsync()).Any(x => x.Name == randomDirectoryName).Should().BeFalse();
        }
    }

    public async Task Should_list_directories_in_root(FtpEncryption encryption)
    {
        using (var sut = new FtpClient(new FtpClientConfiguration
        {
            Host = "ftp.bom.gov.au",
            Username = "anonymous",
            Password = "guest",
            Port = 21,
            EncryptionType = encryption,
            IgnoreCertificateErrors = true
        }))
        {
            string randomDirectoryName = $"{Guid.NewGuid()}";

            await sut.LoginAsync();
            await sut.CreateDirectoryAsync(randomDirectoryName);
            var directories = await sut.ListDirectoriesAsync();

            //directories.Any(x => x.Name == randomDirectoryName).Should().BeTrue();

            await sut.DeleteDirectoryAsync(randomDirectoryName);
        }
    }

    public async Task Should_list_directories_in_subdirectory(FtpEncryption encryption)
    {
        string[] randomDirectoryNames =
        {
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString()
            };
        using (var sut = new FtpClient(new FtpClientConfiguration
        {
            Host = "ftp.bom.gov.au",
            Username = "anonymous",
            Password = "guest",
            Port = 21,
            EncryptionType = encryption,
            IgnoreCertificateErrors = true
        }))
        {

            string joinedPath = string.Join("/", randomDirectoryNames);
            await sut.LoginAsync();

            await sut.CreateDirectoryAsync(joinedPath);
            await sut.ChangeWorkingDirectoryAsync(randomDirectoryNames[0]);
            var directories = await sut.ListDirectoriesAsync();

            //directories.Any(x => x.Name == randomDirectoryNames[1]).Should().BeTrue();

            await sut.ChangeWorkingDirectoryAsync($"/{joinedPath}");
            foreach (string directory in randomDirectoryNames.Reverse())
            {
                await sut.ChangeWorkingDirectoryAsync("../");
                await sut.DeleteDirectoryAsync(directory);
            }
        }
    }

    public async Task Should_be_in_base_directory_when_logging_in(FtpEncryption encryption)
    {
        string randomDirectoryName = $"{Guid.NewGuid()}";

        using (var sut = new FtpClient(new FtpClientConfiguration
        {
            Host = "ftp.bom.gov.au",
            Username = "anonymous",
            Password = "guest",
            Port = 21,
            EncryptionType = encryption,
            IgnoreCertificateErrors = true
        }))
        {
            Debug.Log("----------Logging In first time----------");
            await sut.LoginAsync();
            Debug.Log("----------Creating Directory for use in basepath----------");
            await sut.CreateDirectoryAsync(randomDirectoryName);
        }

        using (var sut = new FtpClient(new FtpClientConfiguration
        {
            Host = "ftp.bom.gov.au",
            Username = "anonymous",
            Password = "guest",
            Port = 21,
            EncryptionType = encryption,
            IgnoreCertificateErrors = true,
            BaseDirectory = randomDirectoryName
        }))
        {
            Debug.Log("----------Logging In second time----------");
            await sut.LoginAsync();
            //sut.WorkingDirectory.Should().Be($"/{randomDirectoryName}");

            Debug.Log("----------Deleting directory----------");
            await sut.DeleteDirectoryAsync($"/{randomDirectoryName}");
        }
    }

    async Task LoginAsync()
    {
        using (var ftpClient = new FtpClient(new FtpClientConfiguration
        {
            Host = "localhost",
            Username = "user",
            Password = "password",
            Port = 990,
            EncryptionType = FtpEncryption.Implicit,
            IgnoreCertificateErrors = true
        }))
        {
            await ftpClient.LoginAsync();
        }
    }

    async Task Download()
    {
        using (var ftpClient = new FtpClient(new FtpClientConfiguration
        {
            Host = "localhost",
            Username = "user",
            Password = "password"
        }))
        {
            var tempFile = new FileInfo("C:\\test.png");
            await ftpClient.LoginAsync();
            using (var ftpReadStream = await ftpClient.OpenFileReadStreamAsync("test.png"))
            {
                using (var fileWriteStream = tempFile.OpenWrite())
                {
                    await ftpReadStream.CopyToAsync(fileWriteStream);
                }
            }
        }
    }

    async Task Upload()
    {
        using (var ftpClient = new FtpClient(new FtpClientConfiguration
        {
            Host = "localhost",
            Username = "user",
            Password = "password"
        }))
        {
            var fileinfo = new FileInfo("C:\\test.png");
            await ftpClient.LoginAsync();

            using (var writeStream = await ftpClient.OpenFileWriteStreamAsync("test.png"))
            {
                var fileReadStream = fileinfo.OpenRead();
                await fileReadStream.CopyToAsync(writeStream);
            }
        }
    }

    public async Task Should_delete_file(FtpEncryption encryption)
    {
        using (var sut = new FtpClient(new FtpClientConfiguration
        {
            Host = "localhost",
            Username = "user",
            Password = "password",
            Port = 990,
            EncryptionType = encryption,
            IgnoreCertificateErrors = true
        }))
        {
            string randomFileName = $"{Guid.NewGuid()}.jpg";
            await sut.LoginAsync();
            //var fileinfo = ResourceHelpers.GetResourceFileInfo("penguin.jpg");

            Debug.Log("Writing the file");
            //using (var writeStream = await sut.OpenFileWriteStreamAsync(randomFileName))
            //{
            //    var fileReadStream = fileinfo.OpenRead();
            //    await fileReadStream.CopyToAsync(writeStream);
            //}

            //Debug.Log("Listing the directory");
            //(await sut.ListFilesAsync()).Any(x => x.Name == randomFileName).Should().BeTrue();

            //Debug.Log("Deleting the file");
            //await sut.DeleteFileAsync(randomFileName);

            //Debug.Log("Listing the firector");
            //(await sut.ListFilesAsync()).Any(x => x.Name == randomFileName).Should().BeFalse();
        }
    }

    public async Task Should_give_size(FtpEncryption encryption)
    {
        using (var sut = new FtpClient(new FtpClientConfiguration
        {
            Host = "localhost",
            Username = "user",
            Password = "password",
            Port = 990,
            EncryptionType = encryption,
            IgnoreCertificateErrors = true
        }))
        {
            string randomFilename = $"{Guid.NewGuid()}.jpg";
            await sut.LoginAsync();

            //await sut.CreateTestResourceWithNameAsync("test.png", randomFilename);
            long size = await sut.GetFileSizeAsync(randomFilename);

            //size.Should().Be(34427);

            await sut.DeleteFileAsync(randomFilename);
        }
    }

    public async Task Should_throw_exception_when_file_nonexistent(FtpEncryption encryption)
    {
        using (var sut = new FtpClient(new FtpClientConfiguration
        {
            Host = "localhost",
            Username = "user",
            Password = "password",
            Port = 990,
            EncryptionType = encryption,
            IgnoreCertificateErrors = true
        }))
        {
            await sut.LoginAsync();

            //await Assert.ThrowsAsync<FtpException>(() => sut.GetFileSizeAsync($"{Guid.NewGuid()}.png"));
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}

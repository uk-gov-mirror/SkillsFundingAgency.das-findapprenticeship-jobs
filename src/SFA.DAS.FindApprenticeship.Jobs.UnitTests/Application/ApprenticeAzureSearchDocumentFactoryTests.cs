using FluentAssertions.Execution;
using SFA.DAS.Encoding;
using SFA.DAS.FindApprenticeship.Jobs.Application;
using SFA.DAS.FindApprenticeship.Jobs.Domain.Documents;
using SFA.DAS.FindApprenticeship.Jobs.Infrastructure.Api.Responses;

namespace SFA.DAS.FindApprenticeship.Jobs.UnitTests.Application;

public class ApprenticeAzureSearchDocumentFactoryTests
{
    [Test, MoqAutoData]
    public void Create_Maps_Foundation_Vacancy(LiveVacancy liveVacancy, ApprenticeAzureSearchDocumentFactory sut)
    {
        // arrange
        liveVacancy.EmploymentLocationOption = AvailableWhere.OneLocation;
        liveVacancy.ApprenticeshipType = ApprenticeshipTypes.Foundation;
        liveVacancy.Skills = null;
        liveVacancy.Qualifications = null;

        // act
        var documents = sut.Create(liveVacancy).ToList();

        // assert
        documents.Should().HaveCount(1);
        var document = documents.Single();
        AssertDocumentIsMappedWithoutAddresses(document, liveVacancy);
    }
    
    [Test, MoqAutoData]
    public void Create_Maps_Deprecated_Address_Style_Vacancy(LiveVacancy liveVacancy, ApprenticeAzureSearchDocumentFactory sut)
    {
        // arrange
        liveVacancy.EmploymentLocationOption = null;
        liveVacancy.EmploymentLocations = null;
        liveVacancy.EmploymentLocationInformation = null;

        // act
        var documents = sut.Create(liveVacancy).ToList();

        // assert
        documents.Should().HaveCount(1);
        var document = documents.Single();
        AssertDocumentIsMappedWithoutAddresses(document, liveVacancy);
        document.Address.Should().BeEquivalentTo(liveVacancy.Address);
        document.AvailableWhere.Should().BeNull();
        document.Id.Should().Be(liveVacancy.Id);
        document.IsPrimaryLocation.Should().BeTrue();
        document.Location.Should().BeEquivalentTo(new { liveVacancy.Address.Latitude, liveVacancy.Address.Longitude });
    }
    
    [Test, MoqAutoData]
    public void Create_Maps_OneLocation_Vacancy(LiveVacancy liveVacancy, ApprenticeAzureSearchDocumentFactory sut)
    {
        // arrange
        var address = liveVacancy.EmploymentLocations!.First();
        liveVacancy.EmploymentLocationOption = AvailableWhere.OneLocation;
        liveVacancy.EmploymentLocations = [address];
        liveVacancy.EmploymentLocationInformation = null;
        liveVacancy.Address = null;

        // act
        var documents = sut.Create(liveVacancy).ToList();

        // assert
        documents.Should().HaveCount(1);
        var document = documents.Single();
        AssertDocumentIsMappedWithoutAddresses(document, liveVacancy);
        document.Address.Should().BeEquivalentTo(address);
        document.AvailableWhere.Should().Be(nameof(AvailableWhere.OneLocation));
        document.Id.Should().Be(liveVacancy.Id);
        document.IsPrimaryLocation.Should().BeTrue();
        document.Location.Should().BeEquivalentTo(new { address.Latitude, address.Longitude });
    }
    
    [Test, MoqAutoData]
    public void Create_Maps_RecruitNationally_Vacancy(LiveVacancy liveVacancy, ApprenticeAzureSearchDocumentFactory sut)
    {
        // arrange
        liveVacancy.EmploymentLocationOption = AvailableWhere.AcrossEngland;
        liveVacancy.EmploymentLocations = null;
        liveVacancy.Address = null;

        // act
        var documents = sut.Create(liveVacancy).ToList();

        // assert
        documents.Should().HaveCount(1);
        var document = documents.Single();
        AssertDocumentIsMappedWithoutAddresses(document, liveVacancy);
        document.Address.Should().BeNull();
        document.AvailableWhere.Should().Be(nameof(AvailableWhere.AcrossEngland));
        document.EmploymentLocationInformation.Should().Be(liveVacancy.EmploymentLocationInformation);
        document.Id.Should().Be(liveVacancy.Id);
        document.IsPrimaryLocation.Should().BeTrue();
        document.Location.Should().BeNull();
    }
    
    [Test, MoqAutoData]
    public void Create_Maps_MultipleLocations_Vacancy(LiveVacancy liveVacancy, ApprenticeAzureSearchDocumentFactory sut)
    {
        // arrange
        liveVacancy.EmploymentLocationOption = AvailableWhere.MultipleLocations;
        liveVacancy.Address = null;
        liveVacancy.EmploymentLocationInformation = null;

        // act
        var documents = sut.Create(liveVacancy).ToList();

        // assert
        documents.Should().HaveCount(liveVacancy.EmploymentLocations!.Count);
        documents.Should().AllSatisfy(document =>
        {
            AssertDocumentIsMappedWithoutAddresses(document, liveVacancy);
            liveVacancy.EmploymentLocations.Should().ContainEquivalentOf(document.Address);
            document.AvailableWhere.Should().Be(nameof(AvailableWhere.MultipleLocations));
            document.EmploymentLocationInformation.Should().BeNull();
            document.Location.Should().BeEquivalentTo(new { document.Address!.Latitude, document.Address.Longitude });
            document.OtherAddresses.Should().NotBeNull();
            document.OtherAddresses.Should().HaveCount(liveVacancy.EmploymentLocations!.Count - 1);
            document.OtherAddresses.Should().NotContainEquivalentOf(document.Address);
        });
        
        documents.First().IsPrimaryLocation.Should().BeTrue();
        documents.Skip(1).Should().AllSatisfy(document => document.IsPrimaryLocation.Should().BeFalse());
    }
    
    [Test, MoqAutoData]
    public void Create_Maps_MultipleLocations_Vacancy_With_Unique_Ids(LiveVacancy liveVacancy, ApprenticeAzureSearchDocumentFactory sut)
    {
        // arrange
        liveVacancy.EmploymentLocationOption = AvailableWhere.MultipleLocations;
        liveVacancy.Address = null;
        liveVacancy.EmploymentLocationInformation = null;

        // act
        var documents = sut.Create(liveVacancy).ToList();

        // assert
        documents.Should().HaveCountGreaterThan(1);
        documents.Select(x => x.Id).Distinct().Count().Should().Be(documents.Count);
        documents.First().AvailableWhere.Should().Be(nameof(AvailableWhere.MultipleLocations));
        documents.First().Id.Should().Be(liveVacancy.Id);
    }
    
    [Test, MoqAutoData]
    public void Create_Deduplicates_Anonymous_MultipleLocations_Vacancy(LiveVacancy liveVacancy, ApprenticeAzureSearchDocumentFactory sut)
    {
        // arrange
        liveVacancy.IsEmployerAnonymous = true;
        liveVacancy.AnonymousEmployerName = "John Smith Ltd";
        liveVacancy.EmploymentLocationOption = AvailableWhere.MultipleLocations;
        liveVacancy.Address = null;
        liveVacancy.EmploymentLocationInformation = null;
        
        liveVacancy.EmploymentLocations = [
            new Address { AddressLine3 = "London", Postcode = "SW1AA", Latitude = 1.2, Longitude = 2.3 },
            new Address { AddressLine3 = "London", Postcode = "SW1AA", Latitude = 1.2, Longitude = 2.3 },
            new Address { AddressLine3 = "London", Postcode = "SW2AA", Latitude = 1.2, Longitude = 2.3 },
            new Address { AddressLine3 = "London", Postcode = "SW2AA", Latitude = 1.2, Longitude = 2.3 },
        ];

        // act
        var documents = sut.Create(liveVacancy).ToList();

        // assert
        documents.Should().HaveCount(2);
        documents.Should().AllSatisfy(document =>
        {
            AssertDocumentIsMappedWithoutAddresses(document, liveVacancy);
            liveVacancy.EmploymentLocations.Should().ContainEquivalentOf(document.Address);
            document.EmploymentLocationInformation.Should().BeNull();
            document.Location.Should().BeEquivalentTo(new { document.Address!.Latitude, document.Address.Longitude });
            document.OtherAddresses.Should().NotBeNull();
            document.OtherAddresses.Should().HaveCount(1);
            document.OtherAddresses.Should().NotContainEquivalentOf(document.Address);
        });
    }
    
    [Test, MoqAutoData]
    public void Create_Decodes_The_Account_And_Legal_Entity_Hashes(
        LiveVacancy liveVacancy,
        [Frozen] Mock<IEncodingService> encodingService,
        ApprenticeAzureSearchDocumentFactory sut)
    {
        // arrange
        encodingService.Setup(x => x.Encode(liveVacancy.AccountId, EncodingType.AccountId)).Returns("888");
        encodingService.Setup(x => x.Encode(liveVacancy.AccountLegalEntityId, EncodingType.PublicAccountLegalEntityId)).Returns("999");

        // act
        var searchDocument = sut.Create(liveVacancy).ToList().First();

        // assert
        searchDocument.AccountPublicHashedId.Should().Be("888");
        searchDocument.AccountLegalEntityPublicHashedId.Should().Be("999");
    }

    private static void AssertDocumentIsMappedWithoutAddresses(ApprenticeAzureSearchDocument document, LiveVacancy source)
    {
        using (new AssertionScope())
        {
            document.ApplicationUrl.Should().Be(source.ApplicationUrl);
            document.ApplicationInstructions.Should().Be(source.ApplicationInstructions);
            document.AccountId.Should().Be(source.AccountId);
            document.AccountLegalEntityId.Should().Be(source.AccountLegalEntityId);
            document.AdditionalQuestion1.Should().Be(source.AdditionalQuestion1);
            document.AdditionalQuestion2.Should().Be(source.AdditionalQuestion2);
            document.AnonymousEmployerName.Should().Be(source.AnonymousEmployerName);
            document.ApplicationMethod.Should().Be(source.ApplicationMethod);
            document.ApprenticeshipLevel.Should().Be(source.ApprenticeshipLevel);
            document.ApprenticeshipType.Should().Be(source.ApprenticeshipType?.ToString() ?? nameof(ApprenticeshipTypes.Standard));
            document.ClosingDate.Should().Be(source.ClosingDate);
            document.Course.Level.Should().Be(source.Level.ToString());
            document.Course.LarsCode.Should().Be(source.StandardLarsCode);
            document.Course.Title.Should().BeEquivalentTo(source.ApprenticeshipTitle);
            document.Course.RouteCode.Should().Be(source.RouteCode);
            document.Description.Should().BeEquivalentTo(source.Description);
            document.EmployerContactEmail.Should().Be(source.EmployerContactEmail);
            document.EmployerContactPhone.Should().Be(source.EmployerContactPhone);
            document.EmployerName.Should().BeEquivalentTo(source.EmployerName);
            document.EmployerWebsiteUrl.Should().Be(source.EmployerWebsiteUrl);
            document.EmployerContactName.Should().Be(source.EmployerContactName);
            document.HoursPerWeek.Should().Be((double)source.Wage.WeeklyHours);
            document.IsDisabilityConfident.Should().Be(source.IsDisabilityConfident);
            document.IsEmployerAnonymous.Should().Be(source.IsEmployerAnonymous);
            document.IsPositiveAboutDisability.Should().Be(source.IsPositiveAboutDisability);
            document.IsRecruitVacancy.Should().Be(source.IsRecruitVacancy);
            document.LongDescription.Should().Be(source.LongDescription);
            document.NumberOfPositions.Should().Be(source.NumberOfPositions);
            document.OutcomeDescription.Should().Be(source.OutcomeDescription);
            document.ProviderName.Should().BeEquivalentTo(source.ProviderName);
            document.Route.Should().BeEquivalentTo(source.Route);
            document.PostedDate.Should().Be(source.PostedDate);

            if (source.ApprenticeshipType == ApprenticeshipTypes.Foundation)
            {
                document.Skills.Should().BeNullOrEmpty();
                document.Qualifications.Should().BeNullOrEmpty();
            }
            else
            {
                document.Skills.Should().BeEquivalentTo(source.Skills);
                document.Qualifications.Should().BeEquivalentTo(source.Qualifications, opt => opt.Excluding(x => x.Weighting));
            }
            document.StartDate.Should().Be(source.StartDate);
            document.ThingsToConsider.Should().Be(source.ThingsToConsider);
            document.Title.Should().BeEquivalentTo(source.Title);
            document.TrainingDescription.Should().Be(source.TrainingDescription);
            document.TypicalJobTitles.Should().BeEquivalentTo(source.TypicalJobTitles);
            document.Ukprn.Should().Be(source.Ukprn.ToString());
            document.VacancyLocationType.Should().Be(source.VacancyLocationType);
            document.VacancyReference.Should().Be($"VAC{source.VacancyReference}");
            document.Wage.Should().NotBeNull();
            document.Wage?.WageAdditionalInformation.Should().BeEquivalentTo(source.Wage.WageAdditionalInformation);
            document.Wage?.WageAmount.Should().Be((long)source.Wage.FixedWageYearlyAmount);
            document.Wage?.WageType.Should().BeEquivalentTo(source.Wage.WageType);
            document.Wage?.WorkingWeekDescription.Should().BeEquivalentTo(source.Wage.WorkingWeekDescription);
            document.Wage?.Duration.Should().Be(source.Wage.Duration);
            document.WageText.Should().Be(source.Wage.WageText);
        }
    }
}
using PinOverlay.Core;

namespace PinOverlay.Core.Tests;

public sealed class RichTextXamlSanitizerTests
{
    private const string Ns = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    private const string Saved =
        $"""<Section xmlns="{Ns}" xml:space="preserve" TextAlignment="Left" LineHeight="Auto" IsHyphenationEnabled="False" xml:lang="ja-jp" FlowDirection="LeftToRight" NumberSubstitution.CultureSource="User" NumberSubstitution.Substitution="AsCulture" FontFamily="Meiryo UI, Yu Gothic UI, Segoe UI" FontStyle="Normal" FontWeight="Normal" FontStretch="Normal" FontSize="24" Foreground="#FFFFFFFF" Typography.StandardLigatures="True"><Paragraph><Run FontWeight="Bold" Foreground="#FFFF0000">こんにちは </Run><Run>world</Run></Paragraph></Section>""";

    [Fact]
    public void Sanitize_KeepsXamlSavedByTextRange()
    {
        var result = RichTextXamlSanitizer.Sanitize(Saved);

        Assert.NotNull(result);
        Assert.Contains("こんにちは </Run>", result);
        Assert.Contains("world", result);
        foreach (var attribute in new[]
        {
            "xml:space=\"preserve\"", "TextAlignment=\"Left\"", "LineHeight=\"Auto\"", "IsHyphenationEnabled=\"False\"",
            "xml:lang=\"ja-jp\"", "FlowDirection=\"LeftToRight\"", "NumberSubstitution.CultureSource=\"User\"",
            "NumberSubstitution.Substitution=\"AsCulture\"", "FontFamily=\"Meiryo UI, Yu Gothic UI, Segoe UI\"",
            "FontStyle=\"Normal\"", "FontWeight=\"Normal\"", "FontStretch=\"Normal\"", "FontSize=\"24\"",
            "Foreground=\"#FFFFFFFF\"", "Typography.StandardLigatures=\"True\"", "FontWeight=\"Bold\"", "Foreground=\"#FFFF0000\"",
        })
        {
            Assert.Contains(attribute, result);
        }
    }

    [Fact]
    public void Sanitize_RejectsObjectDataProviderInResources()
    {
        var xaml = $$"""
            <Section xmlns="{{Ns}}" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" xmlns:sys="clr-namespace:System.Diagnostics;assembly=System">
            <Section.Resources><ObjectDataProvider x:Key="p" ObjectType="{x:Type sys:Process}" MethodName="Start" /></Section.Resources>
            <Paragraph><Run>a</Run></Paragraph></Section>
            """;
        Assert.Null(RichTextXamlSanitizer.Sanitize(xaml));
    }

    [Fact]
    public void Sanitize_RejectsMarkupExtensionValue()
    {
        Assert.Null(RichTextXamlSanitizer.Sanitize($$"""<Section xmlns="{{Ns}}"><Paragraph><Run FontSize="{Binding}">a</Run></Paragraph></Section>"""));
    }

    [Fact]
    public void Sanitize_RejectsClrNamespaceDeclaration()
    {
        Assert.Null(RichTextXamlSanitizer.Sanitize($"""<Section xmlns="{Ns}" xmlns:sys="clr-namespace:System;assembly=System"><Paragraph /></Section>"""));
    }

    [Fact]
    public void Sanitize_RejectsRootOtherThanSection()
    {
        Assert.Null(RichTextXamlSanitizer.Sanitize($"""<Paragraph xmlns="{Ns}"><Run>a</Run></Paragraph>"""));
    }

    [Fact]
    public void Sanitize_RemovesInlineUIContainerAndKeepsText()
    {
        var result = RichTextXamlSanitizer.Sanitize($"""<Section xmlns="{Ns}"><Paragraph><Run>a</Run><InlineUIContainer><Image Source="\\attacker\x.png" /></InlineUIContainer></Paragraph></Section>""");

        Assert.NotNull(result);
        Assert.DoesNotContain("InlineUIContainer", result);
        Assert.DoesNotContain("attacker", result);
        Assert.Contains(">a</Run>", result);
    }

    [Fact]
    public void Sanitize_RemovesPropertyElement()
    {
        var result = RichTextXamlSanitizer.Sanitize($"""<Section xmlns="{Ns}"><Section.Resources><ObjectDataProvider MethodName="Start" /></Section.Resources><Paragraph><Run>a</Run></Paragraph></Section>""");

        Assert.NotNull(result);
        Assert.DoesNotContain("ObjectDataProvider", result);
        Assert.Contains(">a</Run>", result);
    }

    [Fact]
    public void Sanitize_UnwrapsTableAndKeepsCellText()
    {
        var result = RichTextXamlSanitizer.Sanitize($"""<Section xmlns="{Ns}"><Table><Table.Columns><TableColumn Width="100" /></Table.Columns><TableRowGroup><TableRow><TableCell><Paragraph><Run>cell</Run></Paragraph></TableCell></TableRow></TableRowGroup></Table></Section>""");

        Assert.NotNull(result);
        Assert.DoesNotContain("Table", result);
        Assert.Contains("<Paragraph><Run>cell</Run></Paragraph>", result);
    }

    [Fact]
    public void Sanitize_RemovesFontFamilyFileUri()
    {
        var result = RichTextXamlSanitizer.Sanitize($"""<Section xmlns="{Ns}"><Paragraph FontFamily="file://attacker/share/#Probe"><Run>a</Run></Paragraph></Section>""");

        Assert.NotNull(result);
        Assert.DoesNotContain("FontFamily", result);
    }

    [Fact]
    public void Sanitize_KeepsHyperlinkTextWithoutNavigateUri()
    {
        var result = RichTextXamlSanitizer.Sanitize($"""<Section xmlns="{Ns}"><Paragraph><Hyperlink NavigateUri="file:///c:/x.exe"><Run>a</Run></Hyperlink></Paragraph></Section>""");

        Assert.NotNull(result);
        Assert.DoesNotContain("NavigateUri", result);
        Assert.Contains(">a</Run></Hyperlink>", result);
    }

    [Fact]
    public void Sanitize_RejectsDoctype()
    {
        Assert.Null(RichTextXamlSanitizer.Sanitize($"""<!DOCTYPE Section [<!ENTITY e "x">]><Section xmlns="{Ns}"><Paragraph><Run>&e;</Run></Paragraph></Section>"""));
    }

    [Fact]
    public void Sanitize_RejectsProcessingInstruction()
    {
        Assert.Null(RichTextXamlSanitizer.Sanitize($"""<Section xmlns="{Ns}"><?Mapping XmlNamespace="m" ClrNamespace="System.Diagnostics" Assembly="System"?><Paragraph /></Section>"""));
    }

    [Fact]
    public void Sanitize_RemovesContextColor()
    {
        var result = RichTextXamlSanitizer.Sanitize($"""<Section xmlns="{Ns}"><Paragraph><Run Foreground="ContextColor x.icc 1,0,0,0" FontSize="10">a</Run></Paragraph></Section>""");

        Assert.NotNull(result);
        Assert.DoesNotContain("Foreground", result);
        Assert.Contains("FontSize=\"10\"", result);
    }

    [Fact]
    public void Sanitize_RejectsMalformedXml()
    {
        Assert.Null(RichTextXamlSanitizer.Sanitize("<Section"));
    }

    [Fact]
    public void Sanitize_RemovesFontFamilyWithPath()
    {
        var result = RichTextXamlSanitizer.Sanitize($$"""<Section xmlns="{{Ns}}"><Paragraph><Run FontFamily="\\attacker\share\#Font" FontSize="10">a</Run></Paragraph></Section>""");

        Assert.NotNull(result);
        Assert.DoesNotContain("FontFamily", result);
        Assert.Contains("FontSize=\"10\"", result);
        Assert.Contains(">a</Run>", result);
    }

    [Fact]
    public void Sanitize_RemovesAttributeOutsideAllowList()
    {
        var result = RichTextXamlSanitizer.Sanitize($$"""<Section xmlns="{{Ns}}"><Paragraph><Run Cursor="\\attacker\x.cur" FontSize="10">a</Run></Paragraph></Section>""");

        Assert.NotNull(result);
        Assert.DoesNotContain("Cursor", result);
        Assert.Contains("FontSize=\"10\"", result);
        Assert.Contains(">a</Run>", result);
    }

    [Fact]
    public void Sanitize_RemovesInvalidHexColor()
    {
        var result = RichTextXamlSanitizer.Sanitize($"""<Section xmlns="{Ns}"><Paragraph><Run Foreground="#zz" FontSize="10">a</Run></Paragraph></Section>""");

        Assert.NotNull(result);
        Assert.DoesNotContain("Foreground", result);
        Assert.Contains("FontSize=\"10\"", result);
        Assert.Contains(">a</Run>", result);
    }
}

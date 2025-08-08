# check-spelling/check-spelling configuration

| File                          | Purpose                                                                          | Format                                                                                            | Info             |
|-------------------------------|----------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------|------------------|
| [dictionary.txt][10]          | Replacement dictionary (creating this file will override the default dictionary) | one word per line                                                                                 | [dictionary][01] |
| [allow.txt][11]               | Add words to the dictionary                                                      | one word per line (only letters and `'`s allowed)                                                 | [allow][02]      |
| [reject.txt][12]              | Remove words from the dictionary (after allow)                                   | grep pattern matching whole dictionary words                                                      | [reject][03]     |
| [excludes.txt][13]            | Files to ignore entirely                                                         | perl regular expression                                                                           | [excludes][04]   |
| [only.txt][14]                | Only check matching files (applied after excludes)                               | perl regular expression                                                                           | [only][05]       |
| [patterns.txt][15]            | Patterns to ignore from checked lines                                            | perl regular expression (order matters, first match wins)                                         | [patterns][06]   |
| [candidate.patterns][16]      | Patterns that might be worth adding to [patterns.txt][15]                        | perl regular expression with optional comment block introductions (all matches will be suggested) | [candidates][07] |
| [line_forbidden.patterns][17] | Patterns to flag in checked lines                                                | perl regular expression (order matters, first match wins)                                         | [patterns][06]   |
| [expect.txt][18]              | Expected words that aren't in the dictionary                                     | one word per line (sorted, alphabetically)                                                        | [expect][08]     |
| [advice.md][19]               | Supplement for GitHub comment when unrecognized words are found                  | GitHub Markdown                                                                                   | [advice][09]     |

> [!NOTE]
> You can replace any of these files with a directory by the same name
> (minus the suffix) and then include multiple files inside that directory
> (with that suffix) to merge multiple files together.

<!-- Link reference definitions -->
[01]: https://github.com/check-spelling/check-spelling/wiki/Configuration#dictionary
[02]: https://github.com/check-spelling/check-spelling/wiki/Configuration#allow
[03]: https://github.com/check-spelling/check-spelling/wiki/Configuration-Examples%3A-reject
[04]: https://github.com/check-spelling/check-spelling/wiki/Configuration-Examples%3A-excludes
[05]: https://github.com/check-spelling/check-spelling/wiki/Configuration-Examples%3A-only
[06]: https://github.com/check-spelling/check-spelling/wiki/Configuration-Examples%3A-patterns
[07]: https://github.com/check-spelling/check-spelling/wiki/Feature:-Suggest-patterns
[08]: https://github.com/check-spelling/check-spelling/wiki/Configuration#expect
[09]: https://github.com/check-spelling/check-spelling/wiki/Configuration-Examples%3A-advice
[10]: dictionary.txt
[11]: allow.txt
[12]: reject.txt
[13]: excludes.txt
[14]: only.txt
[15]: patterns.txt
[16]: candidate.patterns
[17]: line_forbidden.patterns
[18]: expect.txt
[19]: advice.md

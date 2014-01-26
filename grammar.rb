require 'srgs'

module MusicGrammar
  include Srgs::DSL

  extend self

  grammar 'music' do
    private_rule 'type' do
      one_of do
        item 'artist'
        item 'song'
        item 'album'
        item 'playlist'
      end
    end

    private_rule 'music' do
      item 'Mycroft play the'
      reference 'type'
      tag 'out.type=rules.type;'
      reference_wildcard
      tag 'out.media=rules.latest();'
      item 'on spotify', repeat: "0-1"
    end
  end
end